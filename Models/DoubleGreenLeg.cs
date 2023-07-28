using System;
using System.ComponentModel.DataAnnotations.Schema;
namespace LuqinOfficialAccount.Models
{
	[Table("double_green_leg")]
	public class DoubleGreenLeg
	{
		public DateTime alert_date { get; set; }
		public string gid { get; set; }
		public double price_rise_rate { get; set; }

    }
}

