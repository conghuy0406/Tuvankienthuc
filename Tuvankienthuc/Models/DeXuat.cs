using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;


namespace Tuvankienthuc.Models
{
    public class DeXuat
    {
        [Key]
        public int MaDX { get; set; }

        // FK → Users(Id)
        [Required]
        public int MaSV { get; set; }

        [Required, StringLength(10)]
        public int MaMH { get; set; }  

        [Required, StringLength(2000)]
        public string NoiDung { get; set; } = string.Empty;

        [StringLength(200)]
        public string? Nguon { get; set; }

        public DateTime ThoiGian { get; set; } = DateTime.Now;

        public string? Goal { get; set; }

        // Navigation properties
        [ValidateNever] public User User { get; set; } = null!;
        [ValidateNever] public MonHoc MonHoc { get; set; } = null!;


        [ValidateNever] public ICollection<DeXuatChiTiet> ChiTiets { get; set; } = new List<DeXuatChiTiet>();
    }
}
