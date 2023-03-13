using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace LuqinOfficialAccount.Models
{
	public class Chip
	{
		[Key]
		public int id { get; set; }

		public string gid { get; set; }
		public DateTime alert_date { get; set; }
		public double his_low { get; set; }
		public double his_high { get; set; }
		public double cost_5pct { get; set; }
        public double cost_15pct { get; set; }
        public double cost_50pct { get; set; }
        public double cost_85pct { get; set; }
        public double cost_95pct { get; set; }
    }
}

