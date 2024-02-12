using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace LuqinOfficialAccount.Models
{
	[Table("alert_money_flow")]
	public class MoneyFlow
	{
		public DateTime alert_date { get; set; }
		public string gid { get; set; }
		public double flow_percent { get; set; }
	}
}

