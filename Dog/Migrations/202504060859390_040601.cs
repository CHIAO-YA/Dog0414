namespace Dog.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class _040601 : DbMigration
    {
        public override void Up()
        {
            AlterColumn("dbo.OrderDetails", "DriverID", c => c.Int());
        }
        
        public override void Down()
        {
            AlterColumn("dbo.OrderDetails", "DriverID", c => c.String());
        }
    }
}
