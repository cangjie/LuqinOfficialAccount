using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Humanizer;

namespace LuqinOfficialAccount.Models.Simulator
{
	[Table("simulator_daily_holding")]
	public class SimulatorDailyHolding
    {
		[Key]
        public int      id          { get; set; }
        public int      daily_id    { get; set; }
        public string   gid         { get; set; }
        public string   stock_num   { get; set; }
        public double   stock_value { get; set; }
        public double   stock_cost  { get; set; }

    }
}

