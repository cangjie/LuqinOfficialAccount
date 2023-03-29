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
		public double weight_avg { get; set; }
		public double winner_rate { get; set; }

		public double chipDistribute90
		{
			get
			{
				if (cost_5pct + cost_95pct > 0)
				{
					return (cost_95pct - cost_5pct) / (cost_5pct + cost_95pct);
				}
				else
				{
					return 0;
				}
			}
		}

		public double chipDistribute70
		{
			get
			{
                if (cost_15pct + cost_85pct > 0)
                {
                    return (cost_85pct - cost_15pct) / (cost_85pct + cost_15pct);
                }
                else
                {
                    return 0;
                }
            }
		}



    }
}

