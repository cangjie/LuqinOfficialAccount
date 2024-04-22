using System;
using System.ComponentModel.DataAnnotations.Schema;
namespace LuqinOfficialAccount.Models
{
    [Table("near_line3_big_red")]
    public class NearLine3BigRed
	{
		public DateTime alert_date { get; set; }
		public string gid { get; set; }
		public double open { get; set; }
		public double settle { get; set; }
		public double high { get; set; }
		public double low { get; set; }
		public double rate { get; set; }
		public double line3 { get; set; }

	}
}

