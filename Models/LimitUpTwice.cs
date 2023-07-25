using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace LuqinOfficialAccount.Models
{
    [Table("limit_up_twice")]
    public class LimitUpTwice
	{
		public DateTime alert_date { get; set; }
		public string gid { get; set; }
	}
}

