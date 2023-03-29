using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace LuqinOfficialAccount.Models
{
	[Table("alert_continuous_rise")]
	public class ContinuousRise
	{
		public DateTime alert_date { get; set; }
		public string gid { get; set; }
		public int rise_days { get; set; }
		public double rise_rate { get; set; }
		
	}
}

