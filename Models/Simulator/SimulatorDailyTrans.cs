using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Humanizer;

namespace LuqinOfficialAccount.Models.Simulator
{
	[Table("simulator_daily_trans")]
	public class SimulatorDailyTrans
	{
		[Key]
		public int      id              { get; set; }
        public DateTime trans_date      { get; set; }
        public int      simulator_id    { get; set; }
        public string   gid             { get; set; }
        public string   name            { get; set; }
        public double   trans_price     { get; set; }
        public double   trans_amount    { get; set; }
        public int      stock_num       { get; set; }
	}
}

