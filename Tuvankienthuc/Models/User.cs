using System.ComponentModel.DataAnnotations;

namespace Tuvankienthuc.Models
{
    public class User
    {
        [Key]
        public int Id { get; set; }  // Khóa chính

        [Required]
        [StringLength(150)]
        public string HoTen { get; set; }

        [Required]
        [EmailAddress]
        [StringLength(255)]
        public string Email { get; set; }

        [Required]
        [StringLength(255)]
        public string MatKhau { get; set; } // Lưu mật khẩu dạng hash


        [Required]
        [StringLength(20)]
        public string Role { get; set; }


        [StringLength(50)]
        public string? Lop { get; set; }

        public int? NamHoc { get; set; }

        // Audit
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? UpdatedAt { get; set; }
        public bool IsDeleted { get; set; } = false;
    }
}
