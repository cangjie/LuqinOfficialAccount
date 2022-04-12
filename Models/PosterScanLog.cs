using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace LuqinOfficialAccount.Models
{
    [Table("poster_qrcode_scan_log")]
    public class PosterScanLog
    {
        [Key]
        public int id { get; set; }

        public int poster_user_id { get; set; }
        public int scan_user_id { get; set; }
        public string original_id { get; set; }
        public string open_id { get; set; }

    }
}
