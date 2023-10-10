using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data;

namespace LuqinOfficialAccount.Models.Simulator
{
	[Table("simulator")]
	public class Simulator
	{
		[Key]
		public int      id              { get; set; }
        public double   total_amount    { get; set; }
        public DateTime from_date       { get; set; }
        public DateTime to_date         { get; set; }
        public string   name            { get; set; }
        public string   memo            { get; set; }

    }
}

