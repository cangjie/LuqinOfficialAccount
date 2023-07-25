using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace LuqinOfficialAccount.Models
{
	[Table("alert_above_3_line_for_days")]
	public class Above3Line
	{
		public string gid { get; set; }
		public DateTime alert_date { get; set; }
		public int above_3_line_days { get; set; }

    }
}

