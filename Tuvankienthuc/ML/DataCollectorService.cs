using Microsoft.EntityFrameworkCore;
using Tuvankienthuc.Models;

namespace Tuvankienthuc.ML
{
    public class DataCollectorService
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;

        public DataCollectorService(ApplicationDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        public async Task<string> ExportTrainingDataAsync(int? maMH)
        {
            var query = _context.DeXuatChiTiets
                .Include(x => x.KienThuc)
                .AsQueryable();

            // ⚠️ LỌC TRƯỚC
            if (maMH.HasValue)
            {
                query = query.Where(x => x.KienThuc.MaCD == maMH.Value);
            }

            var data = await query
                .Select(x => new
                {
                    Score = (float)x.Score,
                    DoKho = (float)x.KienThuc.DoKho,
                    SoKienThucTruoc = (float)x.KienThuc.SoKienThucTruoc,
                    TrangThai = (float)(
                        x.KienThuc.KienThucSinhViens.Any(k => k.TrangThai == 2) ? 2 : 0
                    )
                })
                .ToListAsync();

            var folder = Path.Combine(_env.ContentRootPath, "MLData");
            Directory.CreateDirectory(folder);

            var path = Path.Combine(folder, $"train_{DateTime.Now:yyyyMMddHHmmss}.csv");

            using var sw = new StreamWriter(path);
            foreach (var d in data)
            {
                sw.WriteLine($"{d.Score},{d.DoKho},{d.SoKienThucTruoc},{d.TrangThai}");
            }

            return path;
        }
    }
}
