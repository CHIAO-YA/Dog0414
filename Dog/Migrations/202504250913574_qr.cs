namespace Dog.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class qr : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Orders", "RQcodeStatus", c => c.Int(nullable: false));
            DropColumn("dbo.Orders", "Longitude");
            DropColumn("dbo.Orders", "Latitude");
        }
        
        public override void Down()
        {
            AddColumn("dbo.Orders", "Latitude", c => c.Decimal(precision: 18, scale: 6));
            AddColumn("dbo.Orders", "Longitude", c => c.Decimal(precision: 18, scale: 6));
            DropColumn("dbo.Orders", "RQcodeStatus");
        }
    }
}
