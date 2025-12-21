using Tuvankienthuc.Models;
using Tuvankienthuc.Services;

namespace Tuvankienthuc.ViewModels
{
    public class ChiTietTuVanVm
    {
        // Đề xuất
        public DeXuat DeXuat { get; set; } = null!;

        // Timeline học tập
        public List<TimelineVm> Timeline { get; set; } = new();

        // Gợi ý AI theo MaKT
        public Dictionary<int, string> GoiYBoSung { get; set; } = new();
    }
}
