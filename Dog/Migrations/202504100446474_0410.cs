namespace Dog.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class _0410 : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Orders", "Region", c => c.String(maxLength: 50));
            AddColumn("dbo.OrderDetails", "RQcode", c => c.String());
            AddColumn("dbo.Users", "IsOnline", c => c.Boolean(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.Users", "IsOnline");
            DropColumn("dbo.OrderDetails", "RQcode");
            DropColumn("dbo.Orders", "Region");
        }
    }
}
