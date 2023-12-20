using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace LuqinOfficialAccount.Models
{
	
	public class Inflow
	{
		[Key]
		public string gid { get; set; }

		public double inflow { get; set; }

		[NotMapped]
		public DateTime? alert_date { get; set; }

	}
}

