namespace Dog.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class _0426 : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Orders", "QRcodeStatus", c => c.Int());
            DropColumn("dbo.Orders", "RQcodeStatus");
        }
        
        public override void Down()
        {
            AddColumn("dbo.Orders", "RQcodeStatus", c => c.Int());
            DropColumn("dbo.Orders", "QRcodeStatus");
        }
    }
}
