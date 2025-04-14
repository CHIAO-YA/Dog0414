namespace Dog.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class go : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.OrderDetails", "OngoingAt", c => c.DateTime());
            DropColumn("dbo.OrderDetails", "OnTheWayAt");
        }
        
        public override void Down()
        {
            AddColumn("dbo.OrderDetails", "OnTheWayAt", c => c.DateTime());
            DropColumn("dbo.OrderDetails", "OngoingAt");
        }
    }
}
