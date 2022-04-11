using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace LuqinOfficialAccount.Models
{
    [Table("Post")]
    public class Post
    {
        [Key]
        public int id { get; set; }

        public int father_id { get; set; } = 0;
        public int user_id { get; set; }
        public string subject { get; set; } = "";
        public string content_text { get; set; } = "";
        public int verified { get; set; } = 0;

        [NotMapped]
        public string token { get; set; } = "";
        
    }
}
