using Tuvankienthuc.Models;

namespace Tuvankienthuc.ViewModels
{
    public class TaiLieuUpsertVM
    {
        public TaiLieu TaiLieu { get; set; } = new TaiLieu();
        public List<int> SelectedMaCDs { get; set; } = new List<int>();
    }
}
