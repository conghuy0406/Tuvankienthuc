using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Tuvankienthuc.Models
{
    public class KienThucSinhVien
    {
        [Key, Column(Order = 0)]
        public int MaSV { get; set; }

        [Key, Column(Order = 1)]
        public int MaKT { get; set; }

        // 0 = chưa biết, 1 = biết cơ bản, 2 = nắm vững
        public int TrangThai { get; set; } = 0;

        public DateTime? LanHocCuoi { get; set; }

        // Quan hệ
        [ForeignKey("MaSV")]
        public User? user { get; set; }

        [ForeignKey("MaKT")]
        public KienThuc? KienThuc { get; set; }
    }
}
