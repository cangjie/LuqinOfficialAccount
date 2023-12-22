using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace LuqinOfficialAccount.Models
{
	public class FlowList
	{
		public string gid { get; set; }
		public DateTime alert_date { get; set; }
		public int flow_out_days { get; set; }
		public int flow_in_days { get; set; }
		public double flow_rate { get; set; }
	}
}

