namespace Dog.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class OrderDetails : DbMigration
    {
        public override void Up()
        {
            DropForeignKey("dbo.DriverPhotoes", "OrdersID", "dbo.Orders");
            DropForeignKey("dbo.Photos", "OrdersID", "dbo.Orders");
            DropIndex("dbo.DriverPhotoes", new[] { "OrdersID" });
            DropIndex("dbo.Photos", new[] { "OrdersID" });
            CreateTable(
                "dbo.OrderDetails",
                c => new
                    {
                        OrderDetailID = c.Int(nullable: false, identity: true),
                        OrderDetailsNumber = c.String(nullable: false, maxLength: 50),
                        OrdersID = c.Int(nullable: false),
                        ServiceDate = c.DateTime(nullable: false),
                        DriverID = c.String(),
                        OrderStatus = c.Int(),
                        KG = c.Decimal(precision: 18, scale: 2),
                        CommonIssues = c.Int(),
                        IssueDescription = c.String(maxLength: 500),
                        CreatedAt = c.DateTime(),
                        UpdatedAt = c.DateTime(),
                        ReportedAt = c.DateTime(),
                    })
                .PrimaryKey(t => t.OrderDetailID)
                .ForeignKey("dbo.Orders", t => t.OrdersID, cascadeDelete: true)
                .Index(t => t.OrdersID);
            
            AddColumn("dbo.DriverPhotoes", "OrderDetailID", c => c.Int(nullable: false));
            AddColumn("dbo.Photos", "OrderDetailID", c => c.Int(nullable: false));
            CreateIndex("dbo.DriverPhotoes", "OrderDetailID");
            CreateIndex("dbo.Photos", "OrderDetailID");
            AddForeignKey("dbo.DriverPhotoes", "OrderDetailID", "dbo.OrderDetails", "OrderDetailID", cascadeDelete: true);
            AddForeignKey("dbo.Photos", "OrderDetailID", "dbo.OrderDetails", "OrderDetailID", cascadeDelete: true);
            DropColumn("dbo.Orders", "DriverID");
            DropColumn("dbo.Orders", "OrderStatus");
            DropColumn("dbo.Orders", "KG");
            DropColumn("dbo.Orders", "CommonIssues");
            DropColumn("dbo.Orders", "IssueDescription");
            DropColumn("dbo.Orders", "ReportedAt");
            DropColumn("dbo.DriverPhotoes", "OrdersID");
            DropColumn("dbo.Photos", "OrdersID");
        }
        
        public override void Down()
        {
            AddColumn("dbo.Photos", "OrdersID", c => c.Int(nullable: false));
            AddColumn("dbo.DriverPhotoes", "OrdersID", c => c.Int(nullable: false));
            AddColumn("dbo.Orders", "ReportedAt", c => c.DateTime());
            AddColumn("dbo.Orders", "IssueDescription", c => c.String(maxLength: 500));
            AddColumn("dbo.Orders", "CommonIssues", c => c.Int());
            AddColumn("dbo.Orders", "KG", c => c.Decimal(precision: 18, scale: 2));
            AddColumn("dbo.Orders", "OrderStatus", c => c.Int());
            AddColumn("dbo.Orders", "DriverID", c => c.String());
            DropForeignKey("dbo.Photos", "OrderDetailID", "dbo.OrderDetails");
            DropForeignKey("dbo.OrderDetails", "OrdersID", "dbo.Orders");
            DropForeignKey("dbo.DriverPhotoes", "OrderDetailID", "dbo.OrderDetails");
            DropIndex("dbo.Photos", new[] { "OrderDetailID" });
            DropIndex("dbo.OrderDetails", new[] { "OrdersID" });
            DropIndex("dbo.DriverPhotoes", new[] { "OrderDetailID" });
            DropColumn("dbo.Photos", "OrderDetailID");
            DropColumn("dbo.DriverPhotoes", "OrderDetailID");
            DropTable("dbo.OrderDetails");
            CreateIndex("dbo.Photos", "OrdersID");
            CreateIndex("dbo.DriverPhotoes", "OrdersID");
            AddForeignKey("dbo.Photos", "OrdersID", "dbo.Orders", "OrdersID", cascadeDelete: true);
            AddForeignKey("dbo.DriverPhotoes", "OrdersID", "dbo.Orders", "OrdersID", cascadeDelete: true);
        }
    }
}
