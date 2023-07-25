using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace LuqinOfficialAccount.Models
{
	[Table("alert_macd")]
	public class MACD
	{
		public string gid { get; set; }
		public string alert_type { get; set; }
		public DateTime alert_time { get; set; }
		public double alert_price { get; set; }
		public double dif { get; set; }
		public double dea { get; set; }
		public double macd { get; set; }

	}
}

