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

        [Display(Name = "訂單編號")]
        [MaxLength(20)]
        public string OrderNumber { get; set; }

        [Display(Name = "客戶ID")]
        public int UsersID { get; set; }
        [JsonIgnore]//不要理他
        [ForeignKey("UsersID")]//外鍵導航屬性
        public virtual Users Users { get; set; }//UsersID關聯到Users表通過User導航屬性訪問
        //order.User 訪問下單的用戶
        //[Display(Name = "外送員ID")]
        //public string DriverID { get; set; }
        //[JsonIgnore]//不要理他
        //[ForeignKey("DriverID")]//外鍵導航屬性

        //public virtual Users Driver { get; set; }//DriverID也關聯到Users表但通過Driver導航屬性訪問
        //order.Driver 訪問負責接單的外送員

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

        [Display(Name = "訂單者")]
        [MaxLength(50)]
        public string OrderName { get; set; }

        [Display(Name = "下訂單者電話")]
        [MaxLength(20)]
        public string OrderPhone { get; set; }

        [Display(Name = "地址")]
        [MaxLength(255)]
        public string Addresses { get; set; }

        [Display(Name = "區域")]
        [MaxLength(50)]
        public string Region { get; set; }

        [Display(Name = "經度")]
        public decimal? Longitude { get; set; }

        [Display(Name = "緯度")]
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

        //[Display(Name = "訂單狀態")]
        //public OrderStatus? OrderStatus { get; set; }

        [Display(Name = "付款狀態")]
        public PaymentStatus? PaymentStatus { get; set; }


        [Display(Name = "訂單總金額")]
        //[Column(TypeName = "decimal(18, 0)")]
        public decimal TotalAmount { get; set; }


        // Line Pay 相關欄位
        [Display(Name = "LinePay交易ID")]
        public long? LinePayTransactionId { get; set; }  // 由 Line Pay 回傳

        [Display(Name = "LinePay付款方式")]
        [MaxLength(50)]
        public string LinePayMethod { get; set; }  // 例如: CREDIT_CARD, LINE_PAY 等

        [Display(Name = "LinePay交易狀態")]
        [MaxLength(50)]
        public string LinePayStatus { get; set; }  // reserved, confirmed, failed 等

        [Display(Name = "付款確認時間")]
        public DateTime? LinePayConfirmedAt { get; set; }  // 成功確認付款的時間


        







        // 一對多關聯 (一個 Order 有多個 Photo)
        public virtual ICollection<OrderDetails> OrderDetails { get; set; }
        public virtual ICollection<Photo> Photo { get; set; }
        //public virtual ICollection<DriverPhoto> DriverPhoto { get; set; }

    }

}