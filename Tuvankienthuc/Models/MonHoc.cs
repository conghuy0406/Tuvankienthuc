using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Tuvankienthuc.Models
{
  
    public class MonHoc
    {

        public int MaMH { get; set; }


        [Required]
        [StringLength(100)]
        public string TenMH { get; set; }

        public string MoTa { get; set; }

        // ===== Giảng viên phụ trách =====
        public int? GiangVienId { get; set; }

        [ForeignKey("GiangVienId")]
        public User? GiangVien { get; set; }

        // Thuộc tính điều hướng (Navigation Property) để quản lý mối quan hệ 1-nhiều
        public ICollection<ChuDe> ChuDes { get; set; } = new List<ChuDe>();
        public ICollection<DeXuat> DeXuats { get; set; } = new List<DeXuat>();


    }
}