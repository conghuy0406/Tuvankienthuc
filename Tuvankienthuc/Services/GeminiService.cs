using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;

namespace Tuvankienthuc.Services
{
    public class GeminiService
    {
        private readonly HttpClient _http;
        private readonly IConfiguration _config;

        public GeminiService(HttpClient http, IConfiguration config)
        {
            _http = http;
            _config = config;
        }

        // -------------------------------------------------------------------
        // 1. HÀM PUBLIC: Bên ngoài gọi vào hàm này
        // -------------------------------------------------------------------
        public async Task<string> AskAsync(string userMessage)
        {
            // Gọi hàm xử lý riêng ở dưới
            var result = await CallAIAsync(userMessage);

            if (string.IsNullOrEmpty(result))
            {
                return "Hệ thống đang bận, tất cả các AI đều không phản hồi. Vui lòng thử lại sau.";
            }

            return result;
        }

        // -------------------------------------------------------------------
        // 2. HÀM PRIVATE: Chứa danh sách model và logic gọi API
        // -------------------------------------------------------------------
        private async Task<string?> CallAIAsync(string prompt)
        {
            string apiKey = _config["Gemini:ApiKey"];
            if (string.IsNullOrWhiteSpace(apiKey)) return null;

            // Danh sách model theo thứ tự ưu tiên bạn muốn
            string[] models =
            {

                "gemma-2-27b-it",
                "gemma-2-9b-it",

                "gemini-2.5-flash-lite",
                "gemini-1.5-flash-8b",
                "gemini-1.5-flash",      

                "gemini-2.5-flash"

            };

            // Vòng lặp thử từng model
            foreach (var model in models)
            {
                try
                {
                    // Tạo URL cho model hiện tại
                    var url = $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={apiKey}";

                    var requestBody = new
                    {
                        contents = new[]
                        {
                            new { role = "user", parts = new[] { new { text = prompt } } }
                        }
                    };

                    var json = JsonSerializer.Serialize(requestBody);
                    var content = new StringContent(json, Encoding.UTF8, "application/json");

                    // Gửi request
                    var response = await _http.PostAsync(url, content);

                    // Nếu API trả về lỗi (429, 500, 503...) -> Ném lỗi để xuống catch và thử model khác
                    if (!response.IsSuccessStatusCode)
                        throw new Exception($"Model {model} failed with status {response.StatusCode}");

                    var responseText = await response.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(responseText);

                    // Kiểm tra dữ liệu trả về có hợp lệ không
                    if (doc.RootElement.TryGetProperty("candidates", out var candidates) && candidates.GetArrayLength() > 0)
                    {
                        var firstCandidate = candidates[0];

                        // Kiểm tra nếu bị chặn (Safety Filter)
                        if (firstCandidate.TryGetProperty("finishReason", out var finishReason) &&
                            finishReason.GetString() != "STOP")
                        {
                            // Nếu bị chặn nội dung, coi như model này thất bại, thử model khác
                            throw new Exception($"Blocked by Safety: {finishReason.GetString()}");
                        }

                        if (firstCandidate.TryGetProperty("content", out var contentJson) &&
                            contentJson.TryGetProperty("parts", out var parts) &&
                            parts.GetArrayLength() > 0)
                        {
                            var text = parts[0].GetProperty("text").GetString();
                            if (!string.IsNullOrWhiteSpace(text))
                            {
                                // THÀNH CÔNG: Trả về kết quả ngay lập tức
                                return text.Trim();
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Log nhẹ lỗi để debug nếu cần (hoặc bỏ qua để thử model tiếp theo)
                    Console.WriteLine($"⚠️ {model} gặp lỗi: {ex.Message}. Đang chuyển sang model tiếp theo...");
                    continue;
                }
            }
            return null;
        }
    }
}