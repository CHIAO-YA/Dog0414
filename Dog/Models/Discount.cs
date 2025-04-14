using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace Dog.Models
{
    public class Discount
    {
        [Key]
        [Display(Name = "折扣ID")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int DiscountID { get; set; }

        [Display(Name = "月份")]
        public int Months { get; set; }

        [Display(Name = "折數")]
        // [Column(TypeName = "decimal(18,2)")]
        public decimal DiscountRate { get; set; }

        public virtual ICollection<Orders> Orders { get; set; }
    }
}