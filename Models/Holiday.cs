using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace LuqinOfficialAccount.Models
{
    [Table("holidays")]
    public class Holiday
    {
        public DateTime start_date { get; set; }
        public DateTime end_date { get; set; }
    }
}

