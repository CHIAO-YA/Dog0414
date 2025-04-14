using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using Newtonsoft.Json;


namespace Dog.Models
{
    public class DriverPhoto
    {
        [Key]
        [Display(Name = "Dog照片ID")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int DogPhotoID { get; set; }

        [Display(Name = "訂單ID")]
        public int OrderDetailID { get; set; }
        [JsonIgnore]
        [ForeignKey("OrderDetailID")]
        public virtual OrderDetails OrderDetails { get; set; }

        [Display(Name = "接單者拍照")]
        [MaxLength(255)]
        public string DriverImageUrl { get; set; }

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