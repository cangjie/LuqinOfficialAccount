using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace LuqinOfficialAccount.Models
{
    [Table("alert_kdj")]
    public class KDJ
	{
		public string gid { get; set; }
		public string alert_type { get; set; } = "day";
		public DateTime alert_time { get; set; }
		public double alert_price { get; set; }
		public double k { get; set; }
        public double d { get; set; }
        public double j { get; set; }

    }
}

