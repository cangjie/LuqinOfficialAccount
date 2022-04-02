using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace LuqinOfficialAccount.Models
{
    [Table("oa_page_auth_state")]
    public class OAPageAuthState
    {
        [Key]
        public int id { get; set; }
        public string redirect_url { get; set; }
        public int callbacked { get; set; }
    }
}
