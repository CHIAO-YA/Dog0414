using Dog.Migrations;
using System;
using System.Data.Entity;
using System.Linq;

namespace Dog.Models
{
    public class Model1 : DbContext
    {
        public Model1()
            : base("name=Model1")
        {
        }
        public virtual DbSet<Users> Users { get; set; }
        public virtual DbSet<Discount> Discount { get; set; }
        public virtual DbSet<Employee> Employee { get; set; }
        public virtual DbSet<Orders> Orders { get; set; }
        public virtual DbSet<Plan> Plans { get; set; }
        public virtual DbSet<Photo> Photo { get; set; }
        public virtual DbSet<DriverPhoto> DriverPhoto { get; set; }
        public virtual DbSet<OrderDetails> OrderDetails { get; set; }
        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Entity<OrderDetails>().Property(o => o.KG).HasPrecision(18, 2);
            //跟對類別order設定.取得order.公斤的屬性.設定資料類型
            modelBuilder.Entity<Discount>().Property(d => d.DiscountRate).HasPrecision(18, 2);
            // 調用基類的 OnModelCreating，以確保 EF 可以繼續進行其他設定
            modelBuilder.Entity<Orders>().Property(o => o.Longitude).HasPrecision(18, 6);
            modelBuilder.Entity<Orders>().Property(o => o.Latitude).HasPrecision(18, 6);
            modelBuilder.Entity<Orders>().Property(o => o.TotalAmount).HasPrecision(18, 2);
            base.OnModelCreating(modelBuilder);
        }
    }
}