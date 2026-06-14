using Xunit;
using Microsoft.EntityFrameworkCore;
using ShoppingListDiploma.Data;
using ShoppingListDiploma.Models;
using ShoppingListDiploma.Services;
using ShoppingListDiploma.ViewModels;

namespace ShoppingListDiploma.Tests;

public class ShoppingListServiceTests
{
    [Fact]
    public async Task CanAccessGroupAsync_ReturnsTrueOnlyForGroupMember()
    {
        await using var context = CreateContext();
        SeedGroup(context, 1, "owner", "member");
        var service = new ShoppingListService(context);

        Assert.True(await service.CanAccessGroupAsync(1, "member"));
        Assert.False(await service.CanAccessGroupAsync(1, "stranger"));
    }

    [Fact]
    public async Task AddItemAsync_AddsItemForAccessibleActiveList()
    {
        await using var context = CreateContext();
        SeedGroup(context, 1, "owner", "member");
        context.ShoppingLists.Add(new ShoppingList { Id = 10, ShoppingGroupId = 1, Name = "Дом" });
        await context.SaveChangesAsync();
        var service = new ShoppingListService(context);

        var item = await service.AddItemAsync(new ShoppingItemFormViewModel
        {
            ShoppingListId = 10,
            Name = "Молоко",
            Quantity = 2,
            Category = ShoppingItemCategory.Dairy,
            Priority = ShoppingItemPriority.Normal
        }, "member", "не назначен");

        Assert.NotNull(item);
        Assert.Equal("Молоко", await context.ShoppingItems.Select(existingItem => existingItem.Name).SingleAsync());
    }

    [Fact]
    public async Task CanAccessListAsync_DeniesForeignList()
    {
        await using var context = CreateContext();
        SeedGroup(context, 1, "owner", "member");
        SeedGroup(context, 2, "other-owner", "other-member");
        context.ShoppingLists.Add(new ShoppingList { Id = 10, ShoppingGroupId = 1, Name = "Свой" });
        context.ShoppingLists.Add(new ShoppingList { Id = 20, ShoppingGroupId = 2, Name = "Чужой" });
        await context.SaveChangesAsync();
        var service = new ShoppingListService(context);

        Assert.True(await service.CanAccessListAsync(10, "member"));
        Assert.False(await service.CanAccessListAsync(20, "member"));
    }

    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    private static void SeedGroup(ApplicationDbContext context, int groupId, string ownerId, string memberId)
    {
        context.ShoppingGroups.Add(new ShoppingGroup { Id = groupId, Name = $"Group {groupId}", OwnerId = ownerId });
        context.GroupMembers.AddRange(
            new GroupMember { ShoppingGroupId = groupId, ApplicationUserId = ownerId, Role = GroupMemberRole.Owner },
            new GroupMember { ShoppingGroupId = groupId, ApplicationUserId = memberId, Role = GroupMemberRole.Member });
    }
}
