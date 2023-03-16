using System;

namespace LuqinOfficialAccount.Models
{
    public class CountItem
    { 
        public DateTime alert_date { get; set; }
        public string gid { get; set; }
        public string name { get; set; }

        public int days { get; set; }

        public double[] riseRate { get; set; }

        public double totalRiseRate { get; set; }

    }
    public class CountResult
    {
        public int Count { get; set; }
        public int SuccessCount { get; set; }
        public int BigSuccessCount { get; set; }

        public double SuccessRate { get; set; }

        public double BigSuccessRate { get; set; }

        public CountItem[] list { get; set; }

    }
}
