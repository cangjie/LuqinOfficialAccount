using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace LuqinOfficialAccount.Models
{
    [Table("oa_users")]
    public class OAUser
    {
        [Key]
        public int id { get; set; }

        public int user_id { get; set; }
        public string original_id { get; set; }
        public string open_id { get; set; }

    }
}
