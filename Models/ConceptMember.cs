using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace LuqinOfficialAccount.Models
{
    [Table("concept_member")]
    public class ConceptMember
    {
        [Key]
        public int id { get; set; }

        public string concept_code { get; set; }
        public string member_code { get; set; }
        public string member_name { get; set; }

        public string gid
        {
            get
            {
                string code = concept_code;
                switch (code.ToLower().Substring(6, 3))
                {
                    case ".sh":
                        return "sh" + code.Substring(0, 6);
                        break;
                    case ".sz":
                        return "sz" + code.Substring(0, 6);
                        break;
                    default:
                        return code.Trim();
                        break;
                }
                return "";

            }
        }


    }
}
