using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShopAPI.Infrastructure.Migrations;

[DbContext(typeof(ShopDbContext))]
[Migration("20261018000000_SecurityAndIntegrity")]
public sealed class SecurityAndIntegrity : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Legacy data must satisfy these constraints. Backup and resolve collisions first.
        migrationBuilder.Sql("UPDATE \"User\" SET \"Email\" = lower(btrim(\"Email\"));");
        migrationBuilder.Sql("UPDATE \"PromoCodes\" SET \"Code\" = upper(btrim(\"Code\"));");
        migrationBuilder.Sql("DROP INDEX IF EXISTS \"IX_Carts_CustomerId\";");
        migrationBuilder.Sql("DROP INDEX IF EXISTS \"IX_CartItems_CartId\";");
        migrationBuilder.CreateIndex("IX_Carts_CustomerId", "Carts", "CustomerId", unique: true);
        migrationBuilder.CreateIndex("IX_CartItems_CartId_ProductId", "CartItems", new[] { "CartId", "ProductId" }, unique: true);
        migrationBuilder.CreateIndex("IX_User_Email", "User", "Email", unique: true);
        migrationBuilder.CreateIndex("IX_PromoCodes_Code", "PromoCodes", "Code", unique: true);
        migrationBuilder.CreateIndex("IX_Orders_CustomerId", "Orders", "CustomerId");
        migrationBuilder.CreateIndex("IX_RefundItems_RefundRequestId_OrderItemId", "RefundItems", new[] { "RefundRequestId", "OrderItemId" }, unique: true);
        migrationBuilder.Sql("UPDATE \"OrderItems\" SET \"PriceAtPurchase\" = \"Price\" WHERE \"PriceAtPurchase\" = 0 AND \"Price\" > 0;");
        migrationBuilder.Sql("ALTER TABLE \"OrderItems\" ALTER COLUMN \"Price\" TYPE numeric(18,2);");
        migrationBuilder.Sql("ALTER TABLE \"OrderItems\" ALTER COLUMN \"PriceAtPurchase\" TYPE numeric(18,2);");
        migrationBuilder.Sql("ALTER TABLE \"PersonalDiscounts\" ALTER COLUMN \"Value\" TYPE numeric(18,2);");
        migrationBuilder.AddForeignKey("FK_Orders_User_CustomerId", "Orders", "CustomerId", "User", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
        migrationBuilder.Sql("ALTER TABLE \"Products\" ADD CONSTRAINT \"CK_Products_Stock\" CHECK (\"StockQuantity\" >= 0);");
        migrationBuilder.Sql("ALTER TABLE \"Products\" ADD CONSTRAINT \"CK_Products_Price\" CHECK (\"Price\" >= 0);");
        migrationBuilder.Sql("ALTER TABLE \"CartItems\" ADD CONSTRAINT \"CK_CartItems_Quantity\" CHECK (\"Quantity\" > 0);");
        migrationBuilder.Sql("ALTER TABLE \"OrderItems\" ADD CONSTRAINT \"CK_OrderItems_Quantity\" CHECK (\"Quantity\" > 0);");
        migrationBuilder.Sql("ALTER TABLE \"RefundItems\" ADD CONSTRAINT \"CK_RefundItems_Quantity\" CHECK (\"Quantity\" > 0);");
        migrationBuilder.Sql("ALTER TABLE \"PromoCodes\" ADD CONSTRAINT \"CK_PromoCodes_Valid\" CHECK (\"DiscountPercentage\" > 0 AND \"DiscountPercentage\" <= 100 AND \"CurrentUsages\" >= 0 AND \"MaxUsages\" > 0 AND \"CurrentUsages\" <= \"MaxUsages\");");
        migrationBuilder.Sql("ALTER TABLE \"PersonalDiscounts\" ADD CONSTRAINT \"CK_Discounts_Valid\" CHECK (\"Value\" > 0 AND \"StartDate\" < \"EndDate\" AND (\"Type\" <> 0 OR \"Value\" <= 100));");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("ALTER TABLE \"PersonalDiscounts\" DROP CONSTRAINT \"CK_Discounts_Valid\";");
        migrationBuilder.Sql("ALTER TABLE \"PromoCodes\" DROP CONSTRAINT \"CK_PromoCodes_Valid\";");
        migrationBuilder.Sql("ALTER TABLE \"RefundItems\" DROP CONSTRAINT \"CK_RefundItems_Quantity\";");
        migrationBuilder.Sql("ALTER TABLE \"OrderItems\" DROP CONSTRAINT \"CK_OrderItems_Quantity\";");
        migrationBuilder.Sql("ALTER TABLE \"CartItems\" DROP CONSTRAINT \"CK_CartItems_Quantity\";");
        migrationBuilder.Sql("ALTER TABLE \"Products\" DROP CONSTRAINT \"CK_Products_Price\";");
        migrationBuilder.Sql("ALTER TABLE \"Products\" DROP CONSTRAINT \"CK_Products_Stock\";");
        migrationBuilder.DropForeignKey("FK_Orders_User_CustomerId", "Orders");
        migrationBuilder.DropIndex("IX_RefundItems_RefundRequestId_OrderItemId", "RefundItems");
        migrationBuilder.DropIndex("IX_Orders_CustomerId", "Orders");
        migrationBuilder.DropIndex("IX_PromoCodes_Code", "PromoCodes");
        migrationBuilder.DropIndex("IX_User_Email", "User");
        migrationBuilder.DropIndex("IX_CartItems_CartId_ProductId", "CartItems");
        migrationBuilder.DropIndex("IX_Carts_CustomerId", "Carts");
        migrationBuilder.CreateIndex("IX_Carts_CustomerId", "Carts", "CustomerId");
        migrationBuilder.CreateIndex("IX_CartItems_CartId", "CartItems", "CartId");
        migrationBuilder.Sql("ALTER TABLE \"OrderItems\" ALTER COLUMN \"Price\" TYPE numeric;");
        migrationBuilder.Sql("ALTER TABLE \"OrderItems\" ALTER COLUMN \"PriceAtPurchase\" TYPE numeric;");
        migrationBuilder.Sql("ALTER TABLE \"PersonalDiscounts\" ALTER COLUMN \"Value\" TYPE numeric;");
    }
}
