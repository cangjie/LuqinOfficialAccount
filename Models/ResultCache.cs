using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LuqinOfficialAccount.Models
{
    [Table("result_cache")]
    public class ResultCache
	{
		[Key]
		public int id { get; set; }
		public string api_name { get; set; }
		public DateTime alert_date { get; set; }
		public string gid { get; set; }
	}
}

