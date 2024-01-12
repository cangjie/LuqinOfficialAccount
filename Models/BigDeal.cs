using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace LuqinOfficialAccount.Models
{
	[Table("big_deal")]
	public class BigDeal
	{
		public string gid { get; set; }
		public DateTime alert_date { get; set; }
		public double big_deal_vol { get; set; } = 0;
		public double total_vol { get; set; } = 0;
		public double big_deal_amount { get; set; } = 0;
		public double total_amount { get; set; } = 0;
		public double big_deal_ava_price { get; set; } = 0;
		public double u_vol { get; set; } = 0;
		public double d_vol { get; set; } = 0;
		public double e_vol { get; set; } = 0;
		public DateTime update_date { get; set; } = DateTime.Now;

    }
}

