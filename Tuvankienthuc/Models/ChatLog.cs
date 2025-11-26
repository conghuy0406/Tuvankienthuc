using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Tuvankienthuc.Models
{
    public class ChatLog
    {
        public int Id { get; set; }

        public int MaSV { get; set; }

        public int? MaDX { get; set; }

        public string NoiDung { get; set; } = string.Empty;

        public string TraLoi { get; set; } = string.Empty;

        public DateTime ThoiGian { get; set; } = DateTime.Now;

        [ForeignKey(nameof(MaSV))]
        public User? User { get; set; }

        [ForeignKey(nameof(MaDX))]
        public DeXuat? DeXuat { get; set; }
    }
}
