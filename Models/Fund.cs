using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace LuqinOfficialAccount.Models
{
    public class Fund
    {
        public string gid { get; set; }
        public DateTime alert_date { get; set; }
        public double settle { get; set; }
        public double rate { get; set; }
        public double flow_amount { get; set; }
        public double flow_amount_5_avarage { get; set; }
        public double big_flow_amount { get; set; }
        public double big_percent { get; set; }
        public double mid_flow_amount { get; set; }
        public double mid_percent { get; set; }
        public double small_flow_amount { get; set; }
        public double small_percent { get; set; }
    }
}

