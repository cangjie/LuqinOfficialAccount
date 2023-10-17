using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Humanizer;

namespace LuqinOfficialAccount.Models.Simulator
{
	[Table("simulator_daily")]
	public class SimulatorDaily
    {
		[Key]
        public int      id              { get; set; }
        public DateTime trans_date      { get; set; }
        public int      simulator_id    { get; set; }
        public double   stock_amount    { get; set; }
        public double   cash_amount     { get; set; }
        public double   total_amount    { get; set; }

    }
}

