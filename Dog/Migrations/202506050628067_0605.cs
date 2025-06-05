namespace Dog.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class _0605 : DbMigration
    {
        public override void Up()
        {
            AlterColumn("dbo.Orders", "LinePayTransactionId", c => c.String());
        }
        
        public override void Down()
        {
            AlterColumn("dbo.Orders", "LinePayTransactionId", c => c.Long());
        }
    }
}
