namespace Tuvankienthuc.Models
{
    public class BaoCaoVanDe
    {
        public int Id { get; set; }

        // Người gửi (SV / GV)
        public int MaUser { get; set; }
        public User User { get; set; } = null!;

        // Liên quan đề xuất (nullable – có thể là lỗi web)
        public int? MaDX { get; set; }
        public DeXuat? DeXuat { get; set; }

        // Loại vấn đề
        public string LoaiVanDe { get; set; } = "";
        // Ví dụ: "Đề xuất không hợp lý", "Thiếu kiến thức", "Lỗi giao diện", "Bug hệ thống"

        public string TieuDe { get; set; } = "";
        public string NoiDung { get; set; } = "";

        // Trạng thái xử lý
        public bool IsResolved { get; set; } = false;
        public bool IsRead { get; set; } = false;

        public DateTime ThoiGian { get; set; } = DateTime.Now;
    }

}


