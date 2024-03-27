using System;
using System.ComponentModel.DataAnnotations.Schema;
namespace LuqinOfficialAccount.Models
{
	[Table("limit_down")]
	public class LimitDown
	{
		public DateTime alert_date { get; set; }
		public string gid { get; set; }
		public int fake { get; set; } = 0;
	}
}

