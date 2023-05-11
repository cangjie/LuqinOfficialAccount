using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace LuqinOfficialAccount.Models
{
    [Table("concept")]
    public class Concept
    {
        [Key]
        public int Id { get; set; }
        public string code { get; set; }
        public string name { get; set; }
        public DateTime list_date { get; set; }
        public string type { get; set; }
        public DateTime update_date { get; set; } = DateTime.Now;

    }
}
