using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace LuqinOfficialAccount.Models
{
    [Table("users")]
    public class User
    {
        [Key]
        public int id { get; set; }

        public string oa_union_id { get; set; }
    }
}
