using System.ComponentModel.DataAnnotations.Schema;


namespace Tuvankienthuc.Models
{
    public class TaiLieuChuDe
    {
        public int MaTL { get; set; }     
        public int MaCD { get; set; }     
        public short? OrderIndex { get; set; }

        [ForeignKey(nameof(MaTL))]
        public TaiLieu TaiLieu { get; set; }

        [ForeignKey(nameof(MaCD))]
        public ChuDe ChuDe { get; set; }
    }
}
