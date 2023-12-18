using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace LuqinOfficialAccount.Models
{
	[Table("bak_daily_detail")]
	public class BakDailyDetail
	{
		[Key]
		public int id { get; set; }
		public string gid { get; set; }
		public string name { get; set; }
		public DateTime alert_date { get; set; }
		public double price { get; set; }
		public double vol { get; set; }
		public double buying { get; set; }
		public double selling { get; set; }
		public double strength { get; set; }
		public double activity { get; set; }
		public double attack { get; set; }
		public double avg_price { get; set; }
		public double avg_turn_over { get; set; }
		public DateTime create_date { get; set; }
	}
}

