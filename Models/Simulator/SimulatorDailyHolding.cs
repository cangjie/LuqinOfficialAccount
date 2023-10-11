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
        public int      id              { get; set; }
        public DateTime trans_date      { get; set; }
        public int      simulator_id    { get; set; }
        public string   gid             { get; set; }
        public string   name            { get; set; }
        public int      stock_num       { get; set; }
        public double   stock_value     { get; set; }
        public double   stock_cost      { get; set; }
        public int      hold_days       { get; set; }
    }
}

