using System;
using System.ComponentModel.DataAnnotations.Schema;
using Humanizer;
using System.Data;
using System.ComponentModel.DataAnnotations;

namespace LuqinOfficialAccount.Models
{
	[Table("dragon_tiger_list_detail")]
	public class DragonTigerDetail
	{
        [Key]
        public int id { get; set; }

        public int dragon_tiger_list_id { get; set; }
        public DateTime alert_date {get; set;}
        public string gid {get; set;}
        public string exalter {get; set;}
        public double buy {get; set;}
        public double buy_rate {get; set;}
        public double sell {get; set;}
        public double sell_rate {get; set;}
        public double net_buy {get; set;}
        public double side {get; set;}
        public string reason {get; set;}

    }
}

