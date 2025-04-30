namespace Dog.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class _0430 : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Users", "MessageuserId", c => c.String(maxLength: 200));
        }
        
        public override void Down()
        {
            DropColumn("dbo.Users", "MessageuserId");
        }
    }
}
