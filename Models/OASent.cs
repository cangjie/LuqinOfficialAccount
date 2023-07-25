using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace LuqinOfficialAccount.Models
{
    [Table("oa_sent")]
    public class OASent
    {
        [Key]
        public int id { get; set; }

        public string ToUserName { get; set; } = "";
        public string FromUserName { get; set; } = "";
        public int is_service { get; set; } = 0;
        public int origin_message_id { get; set; } = 0;
        public string MsgType { get; set; } = "";
        public string Content { get; set; } = "";
        public string Thumb { get; set; } = "";
        public string err_code { get; set; } = "";
        public string err_msg { get; set; } = "";
        public Article[] articles;

    }

    public class Article
    {
        public string title = "";
        public string description = "";
        public string url = "";
        public string picurl = "";
    }
}
