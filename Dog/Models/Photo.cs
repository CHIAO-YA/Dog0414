using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace Dog.Models
{
    public class Photo
    {
        [Key]
        [Display(Name = "照片ID")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int PhotoID { get; set; }

        [Display(Name = "訂單編號ID")]
        [ForeignKey("Orders")]  // 設定外鍵，關聯到 Orders 表
        public int OrdersID { get; set; }
        public virtual Orders Orders { get; set; }  // 這是導航屬性

        [Display(Name = "放置圖")]
        [MaxLength(255)]
        public string OrderImageUrl { get; set; }

        [Display(Name = "建立日期")]
        [DisplayFormat(ApplyFormatInEditMode = true, DataFormatString = "{0:d}")]
        [DataType(DataType.DateTime)]
        public DateTime? CreatedAt { get; set; }

        [Display(Name = "更新日期")]
        [DisplayFormat(ApplyFormatInEditMode = true, DataFormatString = "{0:d}")]
        [DataType(DataType.DateTime)]
        public DateTime? UpdatedAt { get; set; }
    }
}