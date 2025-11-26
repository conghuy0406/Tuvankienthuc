using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Tuvankienthuc.Models;

namespace Tuvankienthuc.Models
{
    public class TaiLieu
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int MaTL { get; set; }

        [Required, StringLength(200)]
        public string TieuDe { get; set; } = string.Empty;

        public string? MoTa { get; set; }

        // Nếu bạn lưu URL (http/https), giữ [Url].
        // Nếu lưu đường dẫn nội bộ (~/uploads/..), bỏ [Url] để tránh báo lỗi.
        [Required, StringLength(500)]
        // [Url] 
        public string DuongDan { get; set; } = string.Empty;

        [Required, StringLength(50)]
        public string LoaiTL { get; set; } = "Website"; // ví dụ giá trị mặc định

        public DateTime NgayThem { get; set; } = DateTime.Now;


    }
}
