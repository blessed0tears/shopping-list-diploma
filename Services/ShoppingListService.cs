using Microsoft.EntityFrameworkCore;
using ShoppingListDiploma.Data;
using ShoppingListDiploma.Models;
using ShoppingListDiploma.ViewModels;

namespace ShoppingListDiploma.Services;

public class ShoppingListService : IShoppingListService
{
    private readonly ApplicationDbContext _context;

    public ShoppingListService(ApplicationDbContext context)
    {
        _context = context;
    }

    public IQueryable<ShoppingGroup> GetUserGroups(string userId)
    {
        return _context.ShoppingGroups
            .Where(group => group.Members.Any(member => member.ApplicationUserId == userId));
    }

    public IQueryable<ShoppingList> GetUserLists(string userId)
    {
        return _context.ShoppingLists
            .Where(list => list.ShoppingGroup.Members.Any(member => member.ApplicationUserId == userId));
    }

    public IQueryable<ShoppingItem> GetUserItems(string userId)
    {
        return _context.ShoppingItems
            .Include(item => item.HistoryEntries)
            .Where(item => item.ShoppingList.ShoppingGroup.Members.Any(member => member.ApplicationUserId == userId));
    }

    public Task<bool> CanAccessGroupAsync(int groupId, string userId)
    {
        return GetUserGroups(userId).AnyAsync(group => group.Id == groupId);
    }

    public Task<bool> CanAccessListAsync(int listId, string userId)
    {
        return GetUserLists(userId).AnyAsync(list => list.Id == listId);
    }

    public Task<bool> CanManageListAsync(int listId, string userId)
    {
        return _context.ShoppingLists.AnyAsync(list => list.Id == listId
            && (list.ShoppingGroup.OwnerId == userId
                || list.ShoppingGroup.Members.Any(member => member.ApplicationUserId == userId && member.Role == GroupMemberRole.Owner)));
    }

    public async Task<ShoppingList?> CreateListAsync(CreateShoppingListViewModel model, string userId)
    {
        if (!await CanAccessGroupAsync(model.ShoppingGroupId, userId))
        {
            return null;
        }

        var shoppingList = new ShoppingList
        {
            Name = model.Name.Trim(),
            ShoppingGroupId = model.ShoppingGroupId
        };

        _context.ShoppingLists.Add(shoppingList);
        await _context.SaveChangesAsync();
        return shoppingList;
    }

    public async Task<bool> UpdateListAsync(int id, EditShoppingListViewModel model, string userId)
    {
        var shoppingList = await GetUserLists(userId).FirstOrDefaultAsync(list => list.Id == id);
        if (shoppingList is null)
        {
            return false;
        }

        shoppingList.Name = model.Name.Trim();
        if (await CanManageListAsync(id, userId))
        {
            shoppingList.IsArchived = model.IsArchived;
        }

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<ShoppingList?> ToggleArchiveAsync(int id, string userId)
    {
        if (!await CanManageListAsync(id, userId))
        {
            return null;
        }

        var shoppingList = await _context.ShoppingLists.FirstAsync(list => list.Id == id);
        shoppingList.IsArchived = !shoppingList.IsArchived;
        await _context.SaveChangesAsync();
        return shoppingList;
    }

    public async Task<ShoppingList?> DeleteListAsync(int id, string userId)
    {
        if (!await CanManageListAsync(id, userId))
        {
            return null;
        }

        var shoppingList = await _context.ShoppingLists.FirstAsync(list => list.Id == id);
        _context.ShoppingLists.Remove(shoppingList);
        await _context.SaveChangesAsync();
        return shoppingList;
    }

    public async Task<ShoppingItem?> AddItemAsync(ShoppingItemFormViewModel model, string userId, string assignedUserText)
    {
        var shoppingList = await GetUserLists(userId).FirstOrDefaultAsync(list => list.Id == model.ShoppingListId);
        if (shoppingList is null || shoppingList.IsArchived)
        {
            return null;
        }

        var unit = ResolveUnit(model);
        var item = new ShoppingItem
        {
            ShoppingListId = model.ShoppingListId,
            Name = model.Name.Trim(),
            Quantity = model.Quantity,
            Unit = unit,
            Category = model.Category,
            Comment = NormalizeComment(model.Comment),
            Priority = model.Priority,
            EstimatedPrice = model.EstimatedPrice,
            AssignedToUserId = NormalizeAssignedUserId(model.AssignedToUserId),
            CreatedByUserId = userId,
            HistoryEntries =
            {
                new ItemHistory
                {
                    ApplicationUserId = userId,
                    Action = "Created",
                    NewValue = FormatItemValue(model.Name.Trim(), model.Quantity, unit, model.Category, model.Comment, model.Priority, model.EstimatedPrice, assignedUserText)
                }
            }
        };

        _context.ShoppingItems.Add(item);
        await _context.SaveChangesAsync();
        return item;
    }

    public async Task<bool> UpdateItemAsync(int id, ShoppingItemFormViewModel model, string userId, string oldAssignedUserText, string newAssignedUserText)
    {
        var item = await GetUserItems(userId)
            .Include(existingItem => existingItem.ShoppingList)
            .FirstOrDefaultAsync(existingItem => existingItem.Id == id);
        if (item is null || item.ShoppingList.IsArchived)
        {
            return false;
        }

        var oldValue = FormatItemValue(item.Name, item.Quantity, item.Unit, item.Category, item.Comment, item.Priority, item.EstimatedPrice, oldAssignedUserText);
        var unit = ResolveUnit(model);
        item.Name = model.Name.Trim();
        item.Quantity = model.Quantity;
        item.Unit = unit;
        item.Category = model.Category;
        item.Comment = NormalizeComment(model.Comment);
        item.Priority = model.Priority;
        item.EstimatedPrice = model.EstimatedPrice;
        item.AssignedToUserId = NormalizeAssignedUserId(model.AssignedToUserId);
        item.HistoryEntries.Add(new ItemHistory
        {
            ApplicationUserId = userId,
            Action = "Updated",
            OldValue = oldValue,
            NewValue = FormatItemValue(item.Name, item.Quantity, item.Unit, item.Category, item.Comment, item.Priority, item.EstimatedPrice, newAssignedUserText)
        });

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<ShoppingItem?> DeleteItemAsync(int id, string userId)
    {
        var item = await GetUserItems(userId)
            .Include(existingItem => existingItem.ShoppingList)
            .FirstOrDefaultAsync(existingItem => existingItem.Id == id);
        if (item is null || item.ShoppingList.IsArchived)
        {
            return null;
        }

        _context.ShoppingItems.Remove(item);
        await _context.SaveChangesAsync();
        return item;
    }

    public async Task<ShoppingItem?> TogglePurchasedAsync(int id, string userId)
    {
        var item = await GetUserItems(userId)
            .Include(existingItem => existingItem.ShoppingList)
            .FirstOrDefaultAsync(existingItem => existingItem.Id == id);
        if (item is null || item.ShoppingList.IsArchived)
        {
            return null;
        }

        item.IsPurchased = !item.IsPurchased;
        item.PurchasedAtUtc = item.IsPurchased ? DateTime.UtcNow : null;
        item.PurchasedByUserId = item.IsPurchased ? userId : null;
        item.HistoryEntries.Add(new ItemHistory
        {
            ApplicationUserId = userId,
            Action = item.IsPurchased ? "Purchased" : "PurchaseCanceled",
            NewValue = item.Name
        });

        await _context.SaveChangesAsync();
        return item;
    }

    private static string? ResolveUnit(ShoppingItemFormViewModel model)
    {
        var selectedUnit = model.Unit?.Trim();
        if (string.IsNullOrWhiteSpace(selectedUnit)) return null;
        return selectedUnit == ShoppingItemFormViewModel.OtherUnitValue
            ? string.IsNullOrWhiteSpace(model.CustomUnit) ? null : model.CustomUnit.Trim()
            : selectedUnit;
    }

    private static string? NormalizeComment(string? comment) => string.IsNullOrWhiteSpace(comment) ? null : comment.Trim();

    private static string? NormalizeAssignedUserId(string? assignedUserId) => string.IsNullOrWhiteSpace(assignedUserId) ? null : assignedUserId;

    private static string FormatItemValue(string name, decimal quantity, string? unit, string category, string? comment, string priority, decimal? estimatedPrice, string assignedUserText)
    {
        var unitText = string.IsNullOrWhiteSpace(unit) ? "без ед. изм." : unit;
        var commentText = string.IsNullOrWhiteSpace(comment) ? "без комментария" : comment;
        var priceText = estimatedPrice.HasValue ? $"{estimatedPrice.Value:N2} ₽" : "без цены";
        return $"{name}; количество: {quantity} {unitText}; категория: {category}; приоритет: {priority}; цена: {priceText}; ответственный: {assignedUserText}; комментарий: {commentText}";
    }
}
