using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace LuqinOfficialAccount.Models
{
    [Table("token")]
    public class Token
    {
        [Key]
        public int id { get; set; }

        public string token { get; set; }
        public string original_id { get; set; }
        public long expire_timestamp { get; set; }
        public string open_id { get; set; }
        public int state { get; set; }
        public int user_id { get; set; }
    }
}
