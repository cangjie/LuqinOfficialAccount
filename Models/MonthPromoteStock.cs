using System;
using System.ComponentModel.DataAnnotations.Schema;
namespace LuqinOfficialAccount.Models
{
	[Table("month_promote_stock")]
	public class MonthPromoteStock
	{
		public string gid { get; set; }
		public string month { get; set; }
		public string broker { get; set; }
		public string name { get; set; }
	}
}

