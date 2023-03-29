using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace LuqinOfficialAccount.Models
{
    [Table("limit_up")]
    public class LimitUp
    {
        public string gid { get; set; }
        public DateTime alert_date { get; set; }

        [NotMapped]
        public string name { get; set; }
        
    }
}

