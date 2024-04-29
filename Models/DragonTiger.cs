using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace LuqinOfficialAccount.Models
{
	[Table("dragon_tiger_list")]
	public class DragonTiger
	{
        [Key]
        public int id { get; set; }

        public DateTime alert_date {get; set; }
        public string gid {get; set; }
        
        public double close {get; set; }
        public double pct_change {get; set; }
        public double turnover_rate {get; set; }
        public double amount {get; set; }
        public double l_sell {get; set; }
        public double l_buy {get; set; }
        public double l_amount {get; set; }
        public double net_amount {get; set; }
        public double net_rate {get; set; }
        public double amount_rate {get; set; }
        public double float_value {get; set; }
        public string reason {get; set; }

        [NotMapped]
        public List<DragonTigerDetail> details { get; set; }
            

    }
}

