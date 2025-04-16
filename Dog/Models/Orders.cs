using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Numerics;
using System.Web;

namespace Dog.Models
{
    public class Orders
    {
        [Key]
        [Display(Name = "編號ID")]

        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int OrdersID { get; set; }

        [Display(Name = "使用者ID")]
        public int UsersID { get; set; }
        [JsonIgnore]//不要理他
        [ForeignKey("UsersID")]//外鍵導航屬性
        public virtual Users Users { get; set; }

        [Display(Name = "方案ID")]
        public int PlanID { get; set; }
        [JsonIgnore]
        [ForeignKey("PlanID")]
        public virtual Plan Plan { get; set; }//一個 Plan對應 Plan 表/第二個 Plan是屬性
        //這張 Order 表有一個 Plan，用來存放這筆訂單所選的方案

        [Display(Name = "折扣ID")]
        public int DiscountID { get; set; }
        [JsonIgnore]
        [ForeignKey("DiscountID")]
        public virtual Discount Discount { get; set; }

        //[Display(Name = "照片ID")]
        //public int PhotoID { get; set; }

        [Display(Name = "訂單者")]
        [MaxLength(50)]
        public string OrderName { get; set; }

        [Display(Name = "下訂單者電話")]
        [MaxLength(20)]
        public string OrderPhone { get; set; }

        [Display(Name = "地址")]
        [MaxLength(255)]
        public string Addresses { get; set; }

        [Display(Name = "經度")]
        //[Column(TypeName = "decimal(18,6)")]
        public decimal? Longitude { get; set; }
        [Display(Name = "緯度")]
        //[Column(TypeName = "decimal(18,6)")]
        public decimal? Latitude { get; set; }

        [Display(Name = "備註")]
        public string Notes { get; set; }

        [Display(Name = "星期")]
        public string WeekDay { get; set; }

        [Display(Name = "開始日期")]
        public DateTime? StartDate { get; set; }

        [Display(Name = "結束日期")]
        public DateTime? EndDate { get; set; }

        [Display(Name = "建立日期")]
        [DisplayFormat(ApplyFormatInEditMode = true, DataFormatString = "{0:d}")]
        [DataType(DataType.DateTime)]
        public DateTime? CreatedAt { get; set; }

        [Display(Name = "更新日期")]
        [DisplayFormat(ApplyFormatInEditMode = true, DataFormatString = "{0:d}")]
        [DataType(DataType.DateTime)]
        public DateTime? UpdatedAt { get; set; }

        [Display(Name = "訂單狀態")]
        public OrderStatus? OrderStatus { get; set; }

        [Display(Name = "付款狀態")]
        public PaymentStatus? PaymentStatus { get; set; }

        [Display(Name = "公斤數")]
        public decimal? KG { get; set; }

        [Display(Name = "QRcode")]
        public string QRcode { get; set; }

        [Display(Name = "問題描述")]
        [MaxLength(500)]
        public string IssueDescription { get; set; } 

        [Display(Name = "回報時間")]
        public DateTime? ReportedAt { get; set; } = DateTime.Now;

        // 一對多關聯 (一個 Order 有多個 Photo)
        public virtual ICollection<Photo> Photos { get; set; }
    }
}