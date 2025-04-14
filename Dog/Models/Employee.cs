using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace Dog.Models
{
    public class Employee
    {
        [Key]
        [Display(Name = "編號")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int EmployeeID { get; set; }

        [Display(Name = "身分別")]
        public IdentityEnum Identity { get; set; }

        [Index(IsUnique = true)]
        [Required(ErrorMessage = "{0}必填")]
        [Display(Name = "帳號")]
        [MaxLength(100)]
        public string Account { get; set; }

        [Required(ErrorMessage = "{0}必填")]
        [Display(Name = "加密後密碼")]
        [MaxLength(100)]
        public string Password { get; set; }

        [Required(ErrorMessage = "{0}必填")]
        [Display(Name = "加鹽(加密用)")]
        [MaxLength(100)]
        public string Salt { get; set; }

        [Display(Name = "員工姓名")]
        [MaxLength(50)]
        public string Name { get; set; }

        [Display(Name = "員工電話")]
        [MaxLength(50)]
        public string Phone { get; set; }

        [Display(Name = "員工電子郵件")]
        [MaxLength(50)]
        public string Email { get; set; }

        [Display(Name = "創建時間")]
        [DisplayFormat(ApplyFormatInEditMode = true, DataFormatString = "{0:d}")]
        [DataType(DataType.DateTime)]
        public DateTime? CreateDate { get; set; } 
    }
}