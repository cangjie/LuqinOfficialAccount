using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace LuqinOfficialAccount.Models
{
	[Table("big_red")]
	public class BigRed
	{
		public string gid { get; set; }
		public DateTime alert_date { get; set; }
		public double price { get; set; }
		public double rate { get; set; }
	}
}

