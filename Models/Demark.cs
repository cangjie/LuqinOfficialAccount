using System;
using System.ComponentModel.DataAnnotations.Schema;
namespace LuqinOfficialAccount.Models
{
    [Table("alert_demark")]
    public class Demark
    {
        public string gid { get; set; }
        public DateTime alert_time { get; set; }
        public int value { get; set; }

    }
}
