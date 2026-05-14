using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShoppingListDiploma.Data;
using ShoppingListDiploma.Models;
using ShoppingListDiploma.ViewModels;

namespace ShoppingListDiploma.Controllers;

[Authorize]
public class ShoppingListsController : Controller
{
    private readonly ApplicationDbContext _context;

    public ShoppingListsController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var lists = await GetUserLists()
            .AsNoTracking()
            .Include(list => list.ShoppingGroup)
            .OrderBy(list => list.ShoppingGroup.Name)
            .ThenBy(list => list.Name)
            .ToListAsync();

        return View(new ShoppingListsIndexViewModel
        {
            ActiveLists = lists.Where(list => !list.IsArchived).ToList(),
            ArchivedLists = lists.Where(list => list.IsArchived).ToList()
        });
    }

    [HttpGet]
    public async Task<IActionResult> Create(int groupId)
    {
        var group = await GetUserGroups()
            .AsNoTracking()
            .FirstOrDefaultAsync(existingGroup => existingGroup.Id == groupId);

        if (group is null)
        {
            return NotFound();
        }

        ViewData["GroupName"] = group.Name;

        return View(new CreateShoppingListViewModel
        {
            ShoppingGroupId = group.Id
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateShoppingListViewModel model)
    {
        var group = await GetUserGroups()
            .FirstOrDefaultAsync(existingGroup => existingGroup.Id == model.ShoppingGroupId);

        if (group is null)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            ViewData["GroupName"] = group.Name;
            return View(model);
        }

        var shoppingList = new ShoppingList
        {
            Name = model.Name,
            ShoppingGroupId = model.ShoppingGroupId
        };

        _context.ShoppingLists.Add(shoppingList);
        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = "Список создан.";

        return RedirectToAction(nameof(Details), new { id = shoppingList.Id });
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var shoppingList = await GetUserLists()
            .AsNoTracking()
            .FirstOrDefaultAsync(list => list.Id == id);

        if (shoppingList is null)
        {
            return NotFound();
        }

        return View(new EditShoppingListViewModel
        {
            Id = shoppingList.Id,
            ShoppingGroupId = shoppingList.ShoppingGroupId,
            Name = shoppingList.Name,
            IsArchived = shoppingList.IsArchived
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, EditShoppingListViewModel model)
    {
        if (id != model.Id)
        {
            return BadRequest();
        }

        var shoppingList = await GetUserLists()
            .FirstOrDefaultAsync(list => list.Id == id);

        if (shoppingList is null)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            model.ShoppingGroupId = shoppingList.ShoppingGroupId;
            return View(model);
        }

        shoppingList.Name = model.Name;
        shoppingList.IsArchived = model.IsArchived;
        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = "Изменения сохранены.";

        return RedirectToAction(nameof(Details), new { id = shoppingList.Id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleArchive(int id)
    {
        var shoppingList = await GetUserLists()
            .FirstOrDefaultAsync(list => list.Id == id);

        if (shoppingList is null)
        {
            return NotFound();
        }

        shoppingList.IsArchived = !shoppingList.IsArchived;
        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = shoppingList.IsArchived
            ? "Список архивирован."
            : "Список восстановлен из архива.";

        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var shoppingList = await GetUserLists()
            .FirstOrDefaultAsync(list => list.Id == id);

        if (shoppingList is null)
        {
            return NotFound();
        }

        var groupId = shoppingList.ShoppingGroupId;
        _context.ShoppingLists.Remove(shoppingList);
        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = "Список удалён.";

        return RedirectToAction("Details", "ShoppingGroups", new { id = groupId });
    }

    public async Task<IActionResult> Details(int id, string filter = ShoppingItemFilter.All, string? search = null, string? category = null, string sort = ShoppingItemSortOrder.CreatedDesc)
    {
        var shoppingList = await GetUserLists()
            .AsNoTracking()
            .Include(list => list.ShoppingGroup)
            .Include(list => list.Items)
                .ThenInclude(item => item.CreatedByUser)
            .Include(list => list.Items)
                .ThenInclude(item => item.PurchasedByUser)
            .Include(list => list.Items)
                .ThenInclude(item => item.AssignedToUser)
            .FirstOrDefaultAsync(list => list.Id == id);

        if (shoppingList is null)
        {
            return NotFound();
        }

        var normalizedFilter = NormalizeFilter(filter);
        var normalizedCategory = NormalizeCategory(category);
        var normalizedSort = NormalizeSort(sort);
        var normalizedSearch = string.IsNullOrWhiteSpace(search) ? null : search.Trim();
        var items = shoppingList.Items.AsEnumerable();
        items = normalizedFilter switch
        {
            ShoppingItemFilter.Purchased => items.Where(item => item.IsPurchased),
            ShoppingItemFilter.Active => items.Where(item => !item.IsPurchased),
            _ => items
        };

        if (!string.IsNullOrWhiteSpace(normalizedSearch))
        {
            items = items.Where(item => item.Name.Contains(normalizedSearch, StringComparison.CurrentCultureIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(normalizedCategory))
        {
            items = items.Where(item => item.Category == normalizedCategory);
        }

        items = normalizedSort switch
        {
            ShoppingItemSortOrder.CreatedAsc => items.OrderBy(item => item.CreatedAtUtc),
            ShoppingItemSortOrder.NameAsc => items.OrderBy(item => item.Name),
            ShoppingItemSortOrder.NameDesc => items.OrderByDescending(item => item.Name),
            _ => items.OrderByDescending(item => item.CreatedAtUtc)
        };

        var itemIds = shoppingList.Items.Select(item => item.Id).ToList();
        var historyEntries = await _context.ItemHistories
            .AsNoTracking()
            .Include(history => history.ApplicationUser)
            .Include(history => history.ShoppingItem)
            .Where(history => itemIds.Contains(history.ShoppingItemId))
            .OrderByDescending(history => history.CreatedAtUtc)
            .ToListAsync();

        var groupMembers = await GetGroupMembersAsync(shoppingList.ShoppingGroupId);

        return View(new ShoppingListDetailsViewModel
        {
            ShoppingList = shoppingList,
            Filter = normalizedFilter,
            SearchQuery = normalizedSearch,
            CategoryFilter = normalizedCategory,
            SortOrder = normalizedSort,
            TotalEstimatedPrice = shoppingList.Items.Sum(item => item.EstimatedPrice ?? 0),
            PurchasedEstimatedPrice = shoppingList.Items.Where(item => item.IsPurchased).Sum(item => item.EstimatedPrice ?? 0),
            ActiveEstimatedPrice = shoppingList.Items.Where(item => !item.IsPurchased).Sum(item => item.EstimatedPrice ?? 0),
            TotalItemCount = shoppingList.Items.Count,
            Items = items.ToList(),
            GroupMembers = groupMembers,
            HistoryEntries = historyEntries,
            AddItemForm = new ShoppingItemFormViewModel
            {
                ShoppingListId = shoppingList.Id
            }
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddItem([Bind(Prefix = "AddItemForm")] ShoppingItemFormViewModel model)
    {
        var shoppingList = await GetUserLists()
            .FirstOrDefaultAsync(list => list.Id == model.ShoppingListId);

        if (shoppingList is null)
        {
            return NotFound();
        }

        if (shoppingList.IsArchived)
        {
            TempData["ErrorMessage"] = "В архивный список нельзя добавлять товары.";
            return RedirectToAction(nameof(Details), new { id = model.ShoppingListId });
        }

        ValidateUnit(model);
        ValidateCategory(model);
        await ValidateAssignedUserAsync(model, shoppingList.ShoppingGroupId);
        if (!ModelState.IsValid)
        {
            TempData["ErrorMessage"] = "Проверьте данные товара.";
            return RedirectToAction(nameof(Details), new { id = model.ShoppingListId });
        }

        var userId = GetCurrentUserId();
        var unit = ResolveUnit(model);
        var item = new ShoppingItem
        {
            ShoppingListId = model.ShoppingListId,
            Name = model.Name,
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
                    NewValue = FormatItemValue(model.Name, model.Quantity, unit, model.Category, model.Comment, model.Priority, model.EstimatedPrice)
                }
            }
        };

        _context.ShoppingItems.Add(item);
        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = "Товар добавлен.";

        return RedirectToAction(nameof(Details), new { id = model.ShoppingListId });
    }

    [HttpGet]
    public async Task<IActionResult> EditItem(int id)
    {
        var item = await GetUserItems()
            .AsNoTracking()
            .Include(item => item.ShoppingList)
            .FirstOrDefaultAsync(existingItem => existingItem.Id == id);

        if (item is null)
        {
            return NotFound();
        }

        if (item.ShoppingList.IsArchived)
        {
            TempData["ErrorMessage"] = "Архивный список доступен только для просмотра.";
            return RedirectToAction(nameof(Details), new { id = item.ShoppingListId });
        }

        var formModel = CreateItemFormViewModel(item);
        formModel.GroupMembers = await GetGroupMembersAsync(item.ShoppingList.ShoppingGroupId);

        return View(formModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditItem(int id, ShoppingItemFormViewModel model)
    {
        var item = await GetUserItems()
            .Include(item => item.ShoppingList)
            .FirstOrDefaultAsync(existingItem => existingItem.Id == id);

        if (item is null)
        {
            return NotFound();
        }

        if (item.ShoppingList.IsArchived)
        {
            TempData["ErrorMessage"] = "Архивный список доступен только для просмотра.";
            return RedirectToAction(nameof(Details), new { id = item.ShoppingListId });
        }

        ValidateUnit(model);
        ValidateCategory(model);
        await ValidateAssignedUserAsync(model, item.ShoppingList.ShoppingGroupId);
        if (!ModelState.IsValid)
        {
            model.ShoppingListId = item.ShoppingListId;
            model.GroupMembers = await GetGroupMembersAsync(item.ShoppingList.ShoppingGroupId);
            return View(model);
        }

        var userId = GetCurrentUserId();
        var unit = ResolveUnit(model);
        var oldValue = FormatItemValue(item.Name, item.Quantity, item.Unit, item.Category, item.Comment, item.Priority, item.EstimatedPrice);

        item.Name = model.Name;
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
            NewValue = FormatItemValue(item.Name, item.Quantity, item.Unit, item.Category, item.Comment, item.Priority, item.EstimatedPrice)
        });

        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = "Изменения сохранены.";

        return RedirectToAction(nameof(Details), new { id = item.ShoppingListId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteItem(int id)
    {
        var item = await GetUserItems()
            .Include(item => item.ShoppingList)
            .FirstOrDefaultAsync(existingItem => existingItem.Id == id);

        if (item is null)
        {
            return NotFound();
        }

        if (item.ShoppingList.IsArchived)
        {
            TempData["ErrorMessage"] = "Архивный список доступен только для просмотра.";
            return RedirectToAction(nameof(Details), new { id = item.ShoppingListId });
        }

        var listId = item.ShoppingListId;
        _context.ShoppingItems.Remove(item);
        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = "Товар удалён.";

        return RedirectToAction(nameof(Details), new { id = listId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> TogglePurchased(int id)
    {
        var item = await GetUserItems()
            .Include(item => item.ShoppingList)
            .FirstOrDefaultAsync(existingItem => existingItem.Id == id);

        if (item is null)
        {
            return NotFound();
        }

        if (item.ShoppingList.IsArchived)
        {
            TempData["ErrorMessage"] = "Архивный список доступен только для просмотра.";
            return RedirectToAction(nameof(Details), new { id = item.ShoppingListId });
        }

        var userId = GetCurrentUserId();
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
        TempData["SuccessMessage"] = item.IsPurchased
            ? "Товар отмечен как купленный."
            : "Отметка покупки снята.";

        return RedirectToAction(nameof(Details), new { id = item.ShoppingListId });
    }

    private static ShoppingItemFormViewModel CreateItemFormViewModel(ShoppingItem item)
    {
        var isKnownUnit = string.IsNullOrWhiteSpace(item.Unit)
            || ShoppingItemFormViewModel.UnitOptions.Contains(item.Unit);

        return new ShoppingItemFormViewModel
        {
            ShoppingListId = item.ShoppingListId,
            Name = item.Name,
            Quantity = item.Quantity,
            Unit = isKnownUnit ? item.Unit : ShoppingItemFormViewModel.OtherUnitValue,
            CustomUnit = isKnownUnit ? null : item.Unit,
            Category = item.Category,
            Comment = item.Comment,
            Priority = item.Priority,
            EstimatedPrice = item.EstimatedPrice,
            AssignedToUserId = item.AssignedToUserId
        };
    }

    private void ValidateUnit(ShoppingItemFormViewModel model)
    {
        if (model.Unit == ShoppingItemFormViewModel.OtherUnitValue
            && string.IsNullOrWhiteSpace(model.CustomUnit))
        {
            ModelState.AddModelError(nameof(model.CustomUnit), "Введите свою единицу измерения или выберите вариант из списка.");
        }
    }

    private void ValidateCategory(ShoppingItemFormViewModel model)
    {
        if (!ShoppingItemFormViewModel.CategoryOptions.Contains(model.Category))
        {
            ModelState.AddModelError(nameof(model.Category), "Выберите категорию из списка.");
        }

        if (!ShoppingItemFormViewModel.PriorityOptions.Contains(model.Priority))
        {
            ModelState.AddModelError(nameof(model.Priority), "Выберите приоритет из списка.");
        }
    }

    private async Task ValidateAssignedUserAsync(ShoppingItemFormViewModel model, int shoppingGroupId)
    {
        var assignedUserId = NormalizeAssignedUserId(model.AssignedToUserId);
        if (assignedUserId is null)
        {
            return;
        }

        var isGroupMember = await _context.GroupMembers
            .AnyAsync(member => member.ShoppingGroupId == shoppingGroupId
                && member.ApplicationUserId == assignedUserId);
        if (!isGroupMember)
        {
            ModelState.AddModelError(nameof(model.AssignedToUserId), "Выберите ответственного из участников группы.");
        }
    }

    private static string? ResolveUnit(ShoppingItemFormViewModel model)
    {
        var selectedUnit = model.Unit?.Trim();
        if (string.IsNullOrWhiteSpace(selectedUnit))
        {
            return null;
        }

        if (selectedUnit == ShoppingItemFormViewModel.OtherUnitValue)
        {
            return string.IsNullOrWhiteSpace(model.CustomUnit)
                ? null
                : model.CustomUnit.Trim();
        }

        return selectedUnit;
    }

    private static string? NormalizeComment(string? comment)
    {
        return string.IsNullOrWhiteSpace(comment) ? null : comment.Trim();
    }

    private static string? NormalizeAssignedUserId(string? assignedUserId)
    {
        return string.IsNullOrWhiteSpace(assignedUserId) ? null : assignedUserId;
    }

    private static string? NormalizeCategory(string? category)
    {
        if (string.IsNullOrWhiteSpace(category))
        {
            return null;
        }

        return ShoppingItemFormViewModel.CategoryOptions.Contains(category) ? category : null;
    }

    private static string NormalizeSort(string? sort)
    {
        return sort switch
        {
            ShoppingItemSortOrder.CreatedAsc => ShoppingItemSortOrder.CreatedAsc,
            ShoppingItemSortOrder.NameAsc => ShoppingItemSortOrder.NameAsc,
            ShoppingItemSortOrder.NameDesc => ShoppingItemSortOrder.NameDesc,
            _ => ShoppingItemSortOrder.CreatedDesc
        };
    }

    private static string FormatItemValue(string name, decimal quantity, string? unit, string category, string? comment, string priority, decimal? estimatedPrice)
    {
        var unitText = string.IsNullOrWhiteSpace(unit) ? "без ед. изм." : unit;
        var commentText = string.IsNullOrWhiteSpace(comment) ? "без комментария" : comment;
        var priceText = estimatedPrice.HasValue ? $"{estimatedPrice.Value:N2} ₽" : "без цены";

        return $"{name}; количество: {quantity} {unitText}; категория: {category}; приоритет: {priority}; цена: {priceText}; комментарий: {commentText}";
    }

    private async Task<List<GroupMember>> GetGroupMembersAsync(int shoppingGroupId)
    {
        return await _context.GroupMembers
            .AsNoTracking()
            .Include(member => member.ApplicationUser)
            .Where(member => member.ShoppingGroupId == shoppingGroupId)
            .OrderBy(member => member.ApplicationUser.Email)
            .ToListAsync();
    }

    private IQueryable<ShoppingGroup> GetUserGroups()
    {
        var userId = GetCurrentUserId();

        return _context.ShoppingGroups
            .Where(group => group.Members.Any(member => member.ApplicationUserId == userId));
    }

    private IQueryable<ShoppingList> GetUserLists()
    {
        var userId = GetCurrentUserId();

        return _context.ShoppingLists
            .Where(list => list.ShoppingGroup.Members.Any(member => member.ApplicationUserId == userId));
    }

    private IQueryable<ShoppingItem> GetUserItems()
    {
        var userId = GetCurrentUserId();

        return _context.ShoppingItems
            .Include(item => item.HistoryEntries)
            .Where(item => item.ShoppingList.ShoppingGroup.Members.Any(member => member.ApplicationUserId == userId));
    }

    private string GetCurrentUserId()
    {
        return User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new InvalidOperationException("Current user id is not available.");
    }

    private static string NormalizeFilter(string? filter)
    {
        return filter switch
        {
            ShoppingItemFilter.Purchased => ShoppingItemFilter.Purchased,
            ShoppingItemFilter.Active => ShoppingItemFilter.Active,
            _ => ShoppingItemFilter.All
        };
    }
}
