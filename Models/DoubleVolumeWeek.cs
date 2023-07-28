using System;
using System.ComponentModel.DataAnnotations.Schema;
namespace LuqinOfficialAccount.Models
{
    [Table("double_volume_week")]
    public class DoubleVolumeWeek
    {
        public string gid { get; set; }
        public DateTime alert_date { get; set; }

        public double volume_increase { get; set; }
        public double price_increase { get; set; }
        public double high_price { get; set; }
    }
}
