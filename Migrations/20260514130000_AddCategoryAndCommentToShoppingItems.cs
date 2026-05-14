using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShoppingListDiploma.Migrations;

public partial class AddCategoryAndCommentToShoppingItems : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "Category",
            table: "ShoppingItems",
            type: "nvarchar(100)",
            maxLength: 100,
            nullable: false,
            defaultValue: "Другое");

        migrationBuilder.AddColumn<string>(
            name: "Comment",
            table: "ShoppingItems",
            type: "nvarchar(500)",
            maxLength: 500,
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "Category",
            table: "ShoppingItems");

        migrationBuilder.DropColumn(
            name: "Comment",
            table: "ShoppingItems");
    }
}
