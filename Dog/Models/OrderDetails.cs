using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace Dog.Models
{
    public class OrderDetails
    {
        [Key]  // 設定為主鍵
        [Display(Name = "每張訂單ID")]
        public int OrderDetailID { get; set; }  // 訂單詳情 ID

        [Display(Name = "每張訂單編號")]
        [Required]
        [MaxLength(50)]  // 設定最大字元數
        public string OrderDetailsNumber { get; set; }  // 顯示用的訂單詳細編號

        [Display(Name = "訂單編號ID")]
        [ForeignKey("Orders")]  // 設定外鍵，關聯到 Orders 表
        public int OrdersID { get; set; }
        public virtual Orders Orders { get; set; }  // 這是導航屬性

        [Display(Name = "每筆訂單日期")]
        public DateTime ServiceDate { get; set; }  // 每次服務的具體日期

        [Display(Name = "外送員ID")]
        public string DriverID { get; set; }  // 司機 ID


        [Display(Name = "訂單狀態")]
        public OrderStatus? OrderStatus { get; set; }


        [Display(Name = "公斤數")]
        public decimal? KG { get; set; }

        [Display(Name = "常見問題")]
        public CommonIssues? CommonIssues { get; set; }

        [Display(Name = "問題描述")]
        [MaxLength(500)]
        public string IssueDescription { get; set; }

        
        [Display(Name = "建立日期")]
        [DisplayFormat(ApplyFormatInEditMode = true, DataFormatString = "{0:d}")]
        [DataType(DataType.DateTime)]
        public DateTime? CreatedAt { get; set; }

        [Display(Name = "更新日期")]
        [DisplayFormat(ApplyFormatInEditMode = true, DataFormatString = "{0:d}")]
        [DataType(DataType.DateTime)]
        public DateTime? UpdatedAt { get; set; }

        [Display(Name = "回報時間")]
        public DateTime? ReportedAt { get; set; } = DateTime.Now;

        [Display(Name = "QRcode")]
        public string QRcode { get; set; }

        // 顯示用的編號生成邏輯
        public void GenerateOrderDetailsNumber()
        {
            // 設定日期格式為 MMdd（例如：0311）
            string datePart = ServiceDate.ToString("MMdd");

            // 根據訂單 ID 和日期生成編號，例如：O1234-0311
            OrderDetailsNumber = $"O{OrdersID}-{datePart}";
        }
        public virtual ICollection<Photo> Photos { get; set; }
        public virtual ICollection<DriverPhoto> DriverPhoto { get; set; }
    }
}