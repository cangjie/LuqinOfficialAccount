using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace LuqinOfficialAccount.Models
{
	[Table("bak_daily")]
	public class bak_daily
	{
        [Key]
        public int id { get; set; } = 0;
        public string gid {get; set;}
        public DateTime alert_date { get; set; }
        public string name { get; set; } 
        public double pct_change {get; set;}
        public double close {get; set;}
        public double open {get; set;}
        public double high {get; set;}
        public double low {get; set;}
        public double pre_close {get; set;}
        public double vol_ratio {get; set;}
        public double turn_over {get; set;}
        public double vol {get; set;}
        public double selling {get; set;}
        public double buying {get; set;}
        public string indurstry { get; set; }
        public string area { get; set; }
        public double strength {get; set;}
        public double activity {get; set;}

        [NotMapped]
        public double ma5Buying { get; set; } = 0;

        [NotMapped]
        public double ma5Selling { get; set; } = 0;

        [NotMapped]
        public double ma5BSRate { get; set; } = 0;


    }
}

