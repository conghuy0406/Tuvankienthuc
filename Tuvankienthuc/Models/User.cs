using System.ComponentModel.DataAnnotations;

namespace Tuvankienthuc.Models
{
    public class User
    {
        [Key]
        public int Id { get; set; }  // Khóa chính

        // ====== Thông tin chung ======
        [Required]
        [StringLength(150)]
        public string HoTen { get; set; }

        [Required]
        [EmailAddress]
        [StringLength(255)]
        public string Email { get; set; }

        // ====== Mật khẩu (dạng mã hóa HASH) ======
        [Required]
        [StringLength(255)]
        public string MatKhau { get; set; }

        [StringLength(255)]
        public string Salt { get; set; }
        // ====== Phân quyền ======
        // Chỉ gồm 3 loại: Admin / GiangVien / SinhVien
        [Required]
        [StringLength(20)]
        public string Role { get; set; }



        // ====== Trạng thái tài khoản ======
        public bool IsActive { get; set; } = true;   // Khóa / mở tài khoản
        public bool IsDeleted { get; set; } = false; // Xóa mềm

        // ====== Audit ======
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? UpdatedAt { get; set; }
    }
}
