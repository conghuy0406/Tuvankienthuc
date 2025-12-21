namespace Tuvankienthuc.Models
{
    public class DeXuatDanhGia
    {
        public int Id { get; set; }

        public int MaDX { get; set; }
        public DeXuat DeXuat { get; set; } = null!;

        public int MaUser { get; set; }     // SV hoặc GV
        public User User { get; set; } = null!;

        public int Rating { get; set; }     // 1–5
        public string? NhanXet { get; set; }

        public DateTime ThoiGian { get; set; } = DateTime.Now;
        public bool IsRead { get; set; } = false;
    }

}
