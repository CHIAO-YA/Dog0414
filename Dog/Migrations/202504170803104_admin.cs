namespace Dog.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class admin : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.OrderDetails", "UnScheduled", c => c.DateTime());
            AddColumn("dbo.OrderDetails", "ScheduledAt", c => c.DateTime());
            DropColumn("dbo.OrderDetails", "PendingAt");
            DropColumn("dbo.OrderDetails", "CanceledAt");
        }
        
        public override void Down()
        {
            AddColumn("dbo.OrderDetails", "CanceledAt", c => c.DateTime());
            AddColumn("dbo.OrderDetails", "PendingAt", c => c.DateTime());
            DropColumn("dbo.OrderDetails", "ScheduledAt");
            DropColumn("dbo.OrderDetails", "UnScheduled");
        }
    }
}
