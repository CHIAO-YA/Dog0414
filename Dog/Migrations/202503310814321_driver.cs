namespace Dog.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class driver : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Orders", "DriverID", c => c.String());
        }
        
        public override void Down()
        {
            DropColumn("dbo.Orders", "DriverID");
        }
    }
}
