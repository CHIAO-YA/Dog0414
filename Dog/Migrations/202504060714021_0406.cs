namespace Dog.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class _0406 : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Orders", "LinePayTransactionId", c => c.Long());
            AddColumn("dbo.Orders", "LinePayMethod", c => c.String(maxLength: 50));
            AddColumn("dbo.Orders", "LinePayStatus", c => c.String(maxLength: 50));
            AddColumn("dbo.Orders", "LinePayConfirmedAt", c => c.DateTime());
        }
        
        public override void Down()
        {
            DropColumn("dbo.Orders", "LinePayConfirmedAt");
            DropColumn("dbo.Orders", "LinePayStatus");
            DropColumn("dbo.Orders", "LinePayMethod");
            DropColumn("dbo.Orders", "LinePayTransactionId");
        }
    }
}
