namespace Dog.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class _0415 : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.OrderDetails", "ArrivedAt", c => c.DateTime());
        }
        
        public override void Down()
        {
            DropColumn("dbo.OrderDetails", "ArrivedAt");
        }
    }
}
