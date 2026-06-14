using ShoppingListDiploma.Models;
using ShoppingListDiploma.ViewModels;

namespace ShoppingListDiploma.Services;

public interface IShoppingListService
{
    IQueryable<ShoppingGroup> GetUserGroups(string userId);

    IQueryable<ShoppingList> GetUserLists(string userId);

    IQueryable<ShoppingItem> GetUserItems(string userId);

    Task<bool> CanAccessGroupAsync(int groupId, string userId);

    Task<bool> CanAccessListAsync(int listId, string userId);

    Task<bool> CanManageListAsync(int listId, string userId);

    Task<ShoppingList?> CreateListAsync(CreateShoppingListViewModel model, string userId);

    Task<bool> UpdateListAsync(int id, EditShoppingListViewModel model, string userId);

    Task<ShoppingList?> ToggleArchiveAsync(int id, string userId);

    Task<ShoppingList?> DeleteListAsync(int id, string userId);

    Task<ShoppingItem?> AddItemAsync(ShoppingItemFormViewModel model, string userId, string assignedUserText);

    Task<bool> UpdateItemAsync(int id, ShoppingItemFormViewModel model, string userId, string oldAssignedUserText, string newAssignedUserText);

    Task<ShoppingItem?> DeleteItemAsync(int id, string userId);

    Task<ShoppingItem?> TogglePurchasedAsync(int id, string userId);
}
