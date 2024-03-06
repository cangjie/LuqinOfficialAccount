using System;
namespace LuqinOfficialAccount.Models
{
	public class DealCount
	{
		public DealCount()
		{
		}

		public DateTime settleTime { get; set; }
		public string type { get; set; } = "day";
		public long huge_volume { get; set; } = 0;
		public long net_huge_volume { get; set; } = 0;
        public long big_volume { get; set; } = 0;
        public long net_big_volume { get; set; } = 0;
        public long mid_volume { get; set; } = 0;
        public long net_mid_volume { get; set; } = 0;
        public long small_volume { get; set; } = 0;
        public long net_small_volume { get; set; } = 0;
		public double total { get; set; } = 0;
		public long volume { get; set; } = 0;
    }
}

