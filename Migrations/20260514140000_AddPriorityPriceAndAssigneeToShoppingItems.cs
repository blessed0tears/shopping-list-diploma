using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShoppingListDiploma.Migrations;

public partial class AddPriorityPriceAndAssigneeToShoppingItems : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "AssignedToUserId",
            table: "ShoppingItems",
            type: "nvarchar(450)",
            nullable: true);

        migrationBuilder.AddColumn<decimal>(
            name: "EstimatedPrice",
            table: "ShoppingItems",
            type: "decimal(18,2)",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "Priority",
            table: "ShoppingItems",
            type: "nvarchar(50)",
            maxLength: 50,
            nullable: false,
            defaultValue: "Обычный");

        migrationBuilder.CreateIndex(
            name: "IX_ShoppingItems_AssignedToUserId",
            table: "ShoppingItems",
            column: "AssignedToUserId");

        migrationBuilder.AddForeignKey(
            name: "FK_ShoppingItems_AspNetUsers_AssignedToUserId",
            table: "ShoppingItems",
            column: "AssignedToUserId",
            principalTable: "AspNetUsers",
            principalColumn: "Id",
            onDelete: ReferentialAction.SetNull);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_ShoppingItems_AspNetUsers_AssignedToUserId",
            table: "ShoppingItems");

        migrationBuilder.DropIndex(
            name: "IX_ShoppingItems_AssignedToUserId",
            table: "ShoppingItems");

        migrationBuilder.DropColumn(
            name: "AssignedToUserId",
            table: "ShoppingItems");

        migrationBuilder.DropColumn(
            name: "EstimatedPrice",
            table: "ShoppingItems");

        migrationBuilder.DropColumn(
            name: "Priority",
            table: "ShoppingItems");
    }
}
