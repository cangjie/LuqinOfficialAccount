using System;
namespace LuqinOfficialAccount.Models
{
	
	public class KLine
	{
		public DateTime settleTime { get; set; }
		public string type { get; set; } = "day";
		public double open { get; set; } = 0;
		public double settle { get; set; } = 0;
		public double high { get; set; } = 0;
		public double low { get; set; } = 0;
		public long volume { get; set; } = 0;
        public double amount { get; set; } = 0;
        public double rsv { get; set; }  = 0;
        public double k { get; set; } = 0;
        public double d { get; set; } = 0;
        public double j { get; set; } = 0;
        public double dif { get; set; } = 0;
        public double dea { get; set; } = 0;
        public double macd { get; set; } = 0;

    }
}

