using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace Tuvankienthuc.Models
{
    public class ChuDe
    {

        [Key]
        public int MaCD { get; set; }


        [ForeignKey("MonHoc")]
        public int MaMH { get; set; }

        [Required]
        [StringLength(100)]
        public string TenCD { get; set; }

        public string MoTa { get; set; }

        public bool IsKienThucCoBan { get; set; }

        // Thuộc tính điều hướng
        [ValidateNever]
        public MonHoc MonHoc { get; set; }

        // Khởi tạo List để tránh lỗi null reference
        public ICollection<KienThuc> KienThucs { get; set; } = new List<KienThuc>();
    }
}