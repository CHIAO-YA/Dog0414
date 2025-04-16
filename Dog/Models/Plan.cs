using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace Dog.Models
{
    public class Plan
    {
        [Key]
        [Display(Name = "方案")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int PlanID { get; set; }

        [Display(Name = "方案名稱")]
        [MaxLength(20)]
        public string PlanName { get; set; }

        [Display(Name = "公升")]
        [Range(0, 100)]
        public int Liter { get; set; }

        [Display(Name = "價格")]
        [Range(0, 100)]
        public int Price { get; set; }

        //一筆訂單(Order) 只能對應一個方案(Plan)
        //一個方案(Plan) 可以對應多筆訂單(Order)」，一對多
        public virtual ICollection<Orders> Orders { get; set; }
        // 設定關聯：一個 Plan 可能有多個 Order
    }
}