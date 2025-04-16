namespace Dog.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class qr : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.OrderDetails", "QRcode", c => c.String());
            DropColumn("dbo.Orders", "QRcode");
        }
        
        public override void Down()
        {
            AddColumn("dbo.Orders", "QRcode", c => c.String());
            DropColumn("dbo.OrderDetails", "QRcode");
        }
    }
}
