using Microsoft.EntityFrameworkCore;
using Tuvankienthuc.Models;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options) { }

    // ======== BẢNG CHÍNH ========
    public DbSet<User> Users { get; set; }
    public DbSet<MonHoc> MonHocs { get; set; }
    public DbSet<ChuDe> ChuDes { get; set; }
    public DbSet<KienThuc> KienThucs { get; set; }
    public DbSet<KienThucSinhVien> KienThucSinhViens { get; set; }
    public DbSet<DeXuat> DeXuats { get; set; }
    public DbSet<DeXuatChiTiet> DeXuatChiTiets { get; set; }
    public DbSet<TaiLieu> TaiLieus { get; set; }
    public DbSet<TaiLieuChuDe> TaiLieuChuDes { get; set; }
    public DbSet<ChatLog> ChatLogs { get; set; }   // NEW

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ======== USERS ========
        modelBuilder.Entity<User>(e =>
        {
            e.HasKey(u => u.Id);
            e.HasIndex(u => u.Email).IsUnique();
        });

        // ======== MONHOC ========
        modelBuilder.Entity<MonHoc>().HasKey(mh => mh.MaMH);

        // ======== CHUDE ========
        modelBuilder.Entity<ChuDe>().HasKey(cd => cd.MaCD);
        modelBuilder.Entity<ChuDe>()
            .HasOne(cd => cd.MonHoc)
            .WithMany(mh => mh.ChuDes)
            .HasForeignKey(cd => cd.MaMH)
            .OnDelete(DeleteBehavior.Cascade);

        // ======== KIENTHUC ========
        modelBuilder.Entity<KienThuc>().HasKey(kt => kt.MaKT);
        modelBuilder.Entity<KienThuc>()
            .HasOne(kt => kt.ChuDe)
            .WithMany(cd => cd.KienThucs)
            .HasForeignKey(kt => kt.MaCD)
            .OnDelete(DeleteBehavior.Cascade);

        // ======== KIENTHUCSINHVIEN ========
        modelBuilder.Entity<KienThucSinhVien>(e =>
        {
            e.HasKey(k => new { k.MaSV, k.MaKT });

            e.HasOne(k => k.user)
             .WithMany()
             .HasForeignKey(k => k.MaSV)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(k => k.KienThuc)
             .WithMany(kt => kt.KienThucSinhViens)
             .HasForeignKey(k => k.MaKT)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // ======== DEXUAT ========
        modelBuilder.Entity<DeXuat>(e =>
        {
            e.HasKey(dx => dx.MaDX);

            e.HasOne(dx => dx.User)
             .WithMany()
             .HasForeignKey(dx => dx.MaSV)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(dx => dx.MonHoc)
             .WithMany(mh => mh.DeXuats)
             .HasForeignKey(dx => dx.MaMH)
             .OnDelete(DeleteBehavior.NoAction);
        });

        // ======== DEXUATCHITIET ========
        modelBuilder.Entity<DeXuatChiTiet>(e =>
        {
            e.HasKey(x => new { x.MaDX, x.MaKT });

            e.HasOne(x => x.DeXuat)
             .WithMany(dx => dx.ChiTiets)
             .HasForeignKey(x => x.MaDX)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(x => x.KienThuc)
             .WithMany()
             .HasForeignKey(x => x.MaKT)
             .OnDelete(DeleteBehavior.NoAction);
        });

        // ======== TAILIEU ========
        modelBuilder.Entity<TaiLieu>().HasKey(tl => tl.MaTL);

        // ======== TAILIEUCHUDE (bảng trung gian n-n) ========
        modelBuilder.Entity<TaiLieuChuDe>(e =>
        {
            e.ToTable("TaiLieuChuDe");
            e.HasKey(x => new { x.MaTL, x.MaCD });

            e.HasOne(x => x.TaiLieu)
             .WithMany()
             .HasForeignKey(x => x.MaTL)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(x => x.ChuDe)
             .WithMany()
             .HasForeignKey(x => x.MaCD)
             .OnDelete(DeleteBehavior.NoAction);
        });

        // ======== CHATLOGS ========
        modelBuilder.Entity<ChatLog>(e =>
        {
            e.HasKey(c => c.Id);

            e.HasOne(c => c.User)
             .WithMany()
             .HasForeignKey(c => c.MaSV)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(c => c.DeXuat)
             .WithMany()
             .HasForeignKey(c => c.MaDX)
             .OnDelete(DeleteBehavior.SetNull);

            e.Property(c => c.NoiDung).IsRequired();
            e.Property(c => c.TraLoi).IsRequired();
        });
    }
}
