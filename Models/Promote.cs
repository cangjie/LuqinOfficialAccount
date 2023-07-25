using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace LuqinOfficialAccount.Models
{
    [Table("promote_log")]
    public class Promote
    {
        [Key]
        public int id { get; set; }

        public string original_id { get; set; }
        public int promote_user_id { get; set; }
        public string promote_open_id { get; set; }
        public int follow_user_id { get; set; }
        public string follow_open_id { get; set; }
        public DateTime create_date { get; set; } = DateTime.Now;
    }
}
