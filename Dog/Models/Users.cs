using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace Dog.Models
{
    public class Users
    {
        [Key]
        [Display(Name = "編號")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int UsersID { get; set; }

        [Display(Name = "編號")]
        [MaxLength(20)]
        public string Number { get; set; }

        [Display(Name = "使用者LINEID")]
        [MaxLength(200)]
        public string LineId { get; set; }

        [Display(Name = "使用者角色")]
        public Role Roles { get; set; }

        [Display(Name = "Line名稱")]
        [MaxLength(50)]
        public string LineName { get; set; }

        [Display(Name = "Line大頭照")]
        [MaxLength(255)]
        public string LinePicUrl { get; set; }

        [Display(Name = "司機是否上班")]
        public bool IsOnline { get; set; }

        [Display(Name = "建立日期")]
        [DisplayFormat(ApplyFormatInEditMode = true, DataFormatString = "{0:d}")]
        [DataType(DataType.DateTime)]
        public DateTime CreatedAt { get; set; }
        public virtual ICollection<Orders> Orders { get; set; }
        //public virtual ICollection<Orders> DriverOrders { get; set; }
    }
}