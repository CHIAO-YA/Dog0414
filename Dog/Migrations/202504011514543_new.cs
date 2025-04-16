namespace Dog.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class _new : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.Discounts",
                c => new
                    {
                        DiscountID = c.Int(nullable: false, identity: true),
                        Months = c.Int(nullable: false),
                        DiscountRate = c.Decimal(nullable: false, precision: 18, scale: 2),
                    })
                .PrimaryKey(t => t.DiscountID);
            
            CreateTable(
                "dbo.Orders",
                c => new
                    {
                        OrdersID = c.Int(nullable: false, identity: true),
                        OrderNumber = c.String(maxLength: 20),
                        UsersID = c.Int(nullable: false),
                        PlanID = c.Int(nullable: false),
                        DiscountID = c.Int(nullable: false),
                        OrderName = c.String(maxLength: 50),
                        OrderPhone = c.String(maxLength: 20),
                        Addresses = c.String(maxLength: 255),
                        Longitude = c.Decimal(precision: 18, scale: 6),
                        Latitude = c.Decimal(precision: 18, scale: 6),
                        Notes = c.String(),
                        WeekDay = c.String(),
                        StartDate = c.DateTime(),
                        EndDate = c.DateTime(),
                        CreatedAt = c.DateTime(),
                        UpdatedAt = c.DateTime(),
                        PaymentStatus = c.Int(),
                    })
                .PrimaryKey(t => t.OrdersID)
                .ForeignKey("dbo.Discounts", t => t.DiscountID, cascadeDelete: true)
                .ForeignKey("dbo.Plans", t => t.PlanID, cascadeDelete: true)
                .ForeignKey("dbo.Users", t => t.UsersID, cascadeDelete: true)
                .Index(t => t.UsersID)
                .Index(t => t.PlanID)
                .Index(t => t.DiscountID);
            
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
                        QRcode = c.String(),
                    })
                .PrimaryKey(t => t.OrderDetailID)
                .ForeignKey("dbo.Orders", t => t.OrdersID, cascadeDelete: true)
                .Index(t => t.OrdersID);
            
            CreateTable(
                "dbo.DriverPhotoes",
                c => new
                    {
                        DogPhotoID = c.Int(nullable: false, identity: true),
                        OrderDetailID = c.Int(nullable: false),
                        DriverImageUrl = c.String(maxLength: 255),
                        CreatedAt = c.DateTime(),
                        UpdatedAt = c.DateTime(),
                    })
                .PrimaryKey(t => t.DogPhotoID)
                .ForeignKey("dbo.OrderDetails", t => t.OrderDetailID, cascadeDelete: true)
                .Index(t => t.OrderDetailID);
            
            CreateTable(
                "dbo.Photos",
                c => new
                    {
                        PhotoID = c.Int(nullable: false, identity: true),
                        OrdersID = c.Int(nullable: false),
                        OrderImageUrl = c.String(maxLength: 255),
                        CreatedAt = c.DateTime(),
                        UpdatedAt = c.DateTime(),
                    })
                .PrimaryKey(t => t.PhotoID)
                .ForeignKey("dbo.Orders", t => t.OrdersID, cascadeDelete: true)
                .Index(t => t.OrdersID);
            
            CreateTable(
                "dbo.Plans",
                c => new
                    {
                        PlanID = c.Int(nullable: false, identity: true),
                        PlanName = c.String(maxLength: 20),
                        Liter = c.Int(nullable: false),
                        Price = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.PlanID);
            
            CreateTable(
                "dbo.Users",
                c => new
                    {
                        UsersID = c.Int(nullable: false, identity: true),
                        Number = c.String(maxLength: 20),
                        LineId = c.String(maxLength: 200),
                        Roles = c.Int(nullable: false),
                        LineName = c.String(maxLength: 50),
                        LinePicUrl = c.String(maxLength: 255),
                        CreatedAt = c.DateTime(nullable: false),
                    })
                .PrimaryKey(t => t.UsersID);
            
            CreateTable(
                "dbo.Employees",
                c => new
                    {
                        EmployeeID = c.Int(nullable: false, identity: true),
                        Identity = c.Int(nullable: false),
                        Account = c.String(nullable: false, maxLength: 100),
                        Password = c.String(nullable: false, maxLength: 100),
                        Salt = c.String(nullable: false, maxLength: 100),
                        Name = c.String(maxLength: 50),
                        Phone = c.String(maxLength: 50),
                        Email = c.String(maxLength: 50),
                        CreateDate = c.DateTime(),
                    })
                .PrimaryKey(t => t.EmployeeID)
                .Index(t => t.Account, unique: true);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.Orders", "UsersID", "dbo.Users");
            DropForeignKey("dbo.Orders", "PlanID", "dbo.Plans");
            DropForeignKey("dbo.Photos", "OrdersID", "dbo.Orders");
            DropForeignKey("dbo.OrderDetails", "OrdersID", "dbo.Orders");
            DropForeignKey("dbo.DriverPhotoes", "OrderDetailID", "dbo.OrderDetails");
            DropForeignKey("dbo.Orders", "DiscountID", "dbo.Discounts");
            DropIndex("dbo.Employees", new[] { "Account" });
            DropIndex("dbo.Photos", new[] { "OrdersID" });
            DropIndex("dbo.DriverPhotoes", new[] { "OrderDetailID" });
            DropIndex("dbo.OrderDetails", new[] { "OrdersID" });
            DropIndex("dbo.Orders", new[] { "DiscountID" });
            DropIndex("dbo.Orders", new[] { "PlanID" });
            DropIndex("dbo.Orders", new[] { "UsersID" });
            DropTable("dbo.Employees");
            DropTable("dbo.Users");
            DropTable("dbo.Plans");
            DropTable("dbo.Photos");
            DropTable("dbo.DriverPhotoes");
            DropTable("dbo.OrderDetails");
            DropTable("dbo.Orders");
            DropTable("dbo.Discounts");
        }
    }
}
