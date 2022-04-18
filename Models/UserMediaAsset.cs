using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace LuqinOfficialAccount.Models
{
    [Table("user_media_asset")]
    public class UserMediaAsset
    {
        [Key]
        public int id { get; set; }

        public int media_id { get; set; }
        //public string owner_original_id { get; set; }
        public int user_id { get; set; }

    }
}
