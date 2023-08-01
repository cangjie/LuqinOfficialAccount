using System;
using System.ComponentModel.DataAnnotations.Schema;
namespace LuqinOfficialAccount.Models
{
    [Table("first_limit_up_new_high")]
    public class FirstLimitUpNewHigh
    {
        public DateTime alert_date { get; set; }
        public string gid { get; set; }
        public int days { get; set; }
        public double high { get; set; }
        public double low { get; set; }
    }
}
