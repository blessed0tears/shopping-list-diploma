using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShoppingListDiploma.Migrations;

public partial class AddInvitationsAndNotifications : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "GroupInvitations",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                ShoppingGroupId = table.Column<int>(type: "int", nullable: false),
                InvitedUserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                InvitedByUserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                RespondedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_GroupInvitations", x => x.Id);
                table.ForeignKey("FK_GroupInvitations_AspNetUsers_InvitedByUserId", x => x.InvitedByUserId, "AspNetUsers", "Id", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_GroupInvitations_AspNetUsers_InvitedUserId", x => x.InvitedUserId, "AspNetUsers", "Id", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_GroupInvitations_ShoppingGroups_ShoppingGroupId", x => x.ShoppingGroupId, "ShoppingGroups", "Id", onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "Notifications",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                Message = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                Type = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                IsRead = table.Column<bool>(type: "bit", nullable: false),
                CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                RelatedGroupId = table.Column<int>(type: "int", nullable: true),
                RelatedListId = table.Column<int>(type: "int", nullable: true),
                RelatedItemId = table.Column<int>(type: "int", nullable: true),
                RelatedInvitationId = table.Column<int>(type: "int", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Notifications", x => x.Id);
                table.ForeignKey("FK_Notifications_AspNetUsers_UserId", x => x.UserId, "AspNetUsers", "Id", onDelete: ReferentialAction.Cascade);
                table.ForeignKey("FK_Notifications_GroupInvitations_RelatedInvitationId", x => x.RelatedInvitationId, "GroupInvitations", "Id", onDelete: ReferentialAction.SetNull);
                table.ForeignKey("FK_Notifications_ShoppingGroups_RelatedGroupId", x => x.RelatedGroupId, "ShoppingGroups", "Id", onDelete: ReferentialAction.SetNull);
                table.ForeignKey("FK_Notifications_ShoppingItems_RelatedItemId", x => x.RelatedItemId, "ShoppingItems", "Id", onDelete: ReferentialAction.SetNull);
                table.ForeignKey("FK_Notifications_ShoppingLists_RelatedListId", x => x.RelatedListId, "ShoppingLists", "Id", onDelete: ReferentialAction.SetNull);
            });

        migrationBuilder.CreateIndex(name: "IX_GroupInvitations_InvitedByUserId", table: "GroupInvitations", column: "InvitedByUserId");
        migrationBuilder.CreateIndex(name: "IX_GroupInvitations_InvitedUserId", table: "GroupInvitations", column: "InvitedUserId");
        migrationBuilder.CreateIndex(name: "IX_GroupInvitations_ShoppingGroupId_InvitedUserId_Status", table: "GroupInvitations", columns: new[] { "ShoppingGroupId", "InvitedUserId", "Status" });
        migrationBuilder.CreateIndex(name: "IX_Notifications_UserId", table: "Notifications", column: "UserId");
        migrationBuilder.CreateIndex(name: "IX_Notifications_RelatedGroupId", table: "Notifications", column: "RelatedGroupId");
        migrationBuilder.CreateIndex(name: "IX_Notifications_RelatedItemId", table: "Notifications", column: "RelatedItemId");
        migrationBuilder.CreateIndex(name: "IX_Notifications_RelatedListId", table: "Notifications", column: "RelatedListId");
        migrationBuilder.CreateIndex(name: "IX_Notifications_RelatedInvitationId", table: "Notifications", column: "RelatedInvitationId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "Notifications");
        migrationBuilder.DropTable(name: "GroupInvitations");
    }
}
