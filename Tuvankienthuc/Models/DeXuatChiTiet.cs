using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Tuvankienthuc.Models
{
    public class DeXuatChiTiet
    {
        [Key, Column(Order = 0)]
        public int MaDX { get; set; }

        [Key, Column(Order = 1)]
        public int MaKT { get; set; }

        public float Score { get; set; }

        public int RankIndex { get; set; }

        [Column(TypeName = "nvarchar(max)")]
        public string? Reason { get; set; }

        public bool? IsAccepted { get; set; }

        [ForeignKey("MaDX")]
        public DeXuat? DeXuat { get; set; }

        [ForeignKey("MaKT")]
        public KienThuc? KienThuc { get; set; }
    }
}
