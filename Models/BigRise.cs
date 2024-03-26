using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace LuqinOfficialAccount.Models
{
	[Table("big_rise")]
	public class BigRise
	{
		[Key]
		public int id { get; set; }
		public DateTime alert_date { get; set; }
		public string gid { get; set; }
		public double alert_chip { get; set; } = 0;
		public double alert_high { get; set; } = 0;
		public DateTime start_date { get; set; }
		public double start_chip { get; set; } = 0;
		public double start_low { get; set; } = 0;
		public int limit_up_num { get; set; } = 0;
		public int limit_up_twice_num { get; set; } = 0;
		public DateTime update_date { get; set; } = DateTime.Now;
		public DateTime? break_3_line_date { get; set; }

    }
}

