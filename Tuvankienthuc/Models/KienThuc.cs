using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Tuvankienthuc.Models
{
    public class KienThuc
    {
        [Key]
        public int MaKT { get; set; }

        [Required]
        public string NoiDung { get; set; }

        public int MaCD { get; set; }

        public double DoKho { get; set; } = 1;

        public bool IsKienThucCoBan { get; set; } = false;

        // 🔹 Thêm: Câu hỏi tự đánh giá sinh từ Gemini
        [Column(TypeName = "nvarchar(max)")]
        public string? CauHoiAI { get; set; }

        public int? MucDoCauHoi { get; set; }

        // 🔹 Số kiến thức cần biết trước (cho ML.NET)
        public int SoKienThucTruoc { get; set; } = 0;

        // Quan hệ
        [ForeignKey("MaCD")]
        public ChuDe? ChuDe { get; set; }

        public ICollection<KienThucSinhVien>? KienThucSinhViens { get; set; }
    }
}
