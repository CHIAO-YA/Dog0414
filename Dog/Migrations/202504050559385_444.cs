namespace Dog.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class _444 : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Plans", "PlanPeople", c => c.String(maxLength: 20));
            DropColumn("dbo.Plans", "PlanPleplo");
        }
        
        public override void Down()
        {
            AddColumn("dbo.Plans", "PlanPleplo", c => c.String(maxLength: 20));
            DropColumn("dbo.Plans", "PlanPeople");
        }
    }
}
