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

    public async Task<IActionResult> Details(int id, string filter = ShoppingItemFilter.All)
    {
        var shoppingList = await GetUserLists()
            .AsNoTracking()
            .Include(list => list.ShoppingGroup)
            .Include(list => list.Items)
                .ThenInclude(item => item.CreatedByUser)
            .Include(list => list.Items)
                .ThenInclude(item => item.PurchasedByUser)
            .FirstOrDefaultAsync(list => list.Id == id);

        if (shoppingList is null)
        {
            return NotFound();
        }

        var normalizedFilter = NormalizeFilter(filter);
        var items = shoppingList.Items.AsEnumerable();
        items = normalizedFilter switch
        {
            ShoppingItemFilter.Purchased => items.Where(item => item.IsPurchased),
            ShoppingItemFilter.Active => items.Where(item => !item.IsPurchased),
            _ => items
        };

        var itemIds = shoppingList.Items.Select(item => item.Id).ToList();
        var historyEntries = await _context.ItemHistories
            .AsNoTracking()
            .Include(history => history.ApplicationUser)
            .Include(history => history.ShoppingItem)
            .Where(history => itemIds.Contains(history.ShoppingItemId))
            .OrderByDescending(history => history.CreatedAtUtc)
            .ToListAsync();

        return View(new ShoppingListDetailsViewModel
        {
            ShoppingList = shoppingList,
            Filter = normalizedFilter,
            Items = items.OrderBy(item => item.IsPurchased).ThenBy(item => item.Name).ToList(),
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
            CreatedByUserId = userId,
            HistoryEntries =
            {
                new ItemHistory
                {
                    ApplicationUserId = userId,
                    Action = "Created",
                    NewValue = FormatItemValue(model.Name, model.Quantity, unit, model.Category, model.Comment)
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

        return View(CreateItemFormViewModel(item));
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
        if (!ModelState.IsValid)
        {
            model.ShoppingListId = item.ShoppingListId;
            return View(model);
        }

        var userId = GetCurrentUserId();
        var unit = ResolveUnit(model);
        var oldValue = FormatItemValue(item.Name, item.Quantity, item.Unit, item.Category, item.Comment);

        item.Name = model.Name;
        item.Quantity = model.Quantity;
        item.Unit = unit;
        item.Category = model.Category;
        item.Comment = NormalizeComment(model.Comment);
        item.HistoryEntries.Add(new ItemHistory
        {
            ApplicationUserId = userId,
            Action = "Updated",
            OldValue = oldValue,
            NewValue = FormatItemValue(item.Name, item.Quantity, item.Unit, item.Category, item.Comment)
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
            Comment = item.Comment
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

    private static string FormatItemValue(string name, decimal quantity, string? unit, string category, string? comment)
    {
        var unitText = string.IsNullOrWhiteSpace(unit) ? "без ед. изм." : unit;
        var commentText = string.IsNullOrWhiteSpace(comment) ? "без комментария" : comment;

        return $"{name}; количество: {quantity} {unitText}; категория: {category}; комментарий: {commentText}";
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
