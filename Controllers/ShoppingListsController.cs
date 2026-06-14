using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShoppingListDiploma.Data;
using ShoppingListDiploma.Models;
using ShoppingListDiploma.ViewModels;
using ShoppingListDiploma.Services;

namespace ShoppingListDiploma.Controllers;

[Authorize]
public class ShoppingListsController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IShoppingListService _shoppingListService;

    public ShoppingListsController(ApplicationDbContext context, IShoppingListService shoppingListService)
    {
        _context = context;
        _shoppingListService = shoppingListService;
    }

    public async Task<IActionResult> Index()
    {
        var lists = await _shoppingListService.GetUserLists(GetCurrentUserId())
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
        var group = await _shoppingListService.GetUserGroups(GetCurrentUserId())
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
        var group = await _shoppingListService.GetUserGroups(GetCurrentUserId())
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

        var shoppingList = await _shoppingListService.CreateListAsync(model, GetCurrentUserId());
        if (shoppingList is null)
        {
            return NotFound();
        }
        TempData["SuccessMessage"] = "Список создан.";

        return RedirectToAction(nameof(Details), new { id = shoppingList.Id });
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var shoppingList = await _shoppingListService.GetUserLists(GetCurrentUserId())
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

        var shoppingList = await _shoppingListService.GetUserLists(GetCurrentUserId())
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

        if (!await _shoppingListService.UpdateListAsync(id, model, GetCurrentUserId()))
        {
            return NotFound();
        }
        TempData["SuccessMessage"] = "Изменения сохранены.";

        return RedirectToAction(nameof(Details), new { id = shoppingList.Id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleArchive(int id)
    {
        var shoppingList = await _shoppingListService.GetUserLists(GetCurrentUserId())
            .FirstOrDefaultAsync(list => list.Id == id);

        if (shoppingList is null)
        {
            return NotFound();
        }

        if (!await _shoppingListService.CanManageListAsync(id, GetCurrentUserId()))
        {
            return Forbid();
        }

        shoppingList = await _shoppingListService.ToggleArchiveAsync(id, GetCurrentUserId());
        if (shoppingList is null)
        {
            return NotFound();
        }
        TempData["SuccessMessage"] = shoppingList.IsArchived
            ? "Список архивирован."
            : "Список восстановлен из архива.";

        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var shoppingList = await _shoppingListService.GetUserLists(GetCurrentUserId())
            .FirstOrDefaultAsync(list => list.Id == id);

        if (shoppingList is null)
        {
            return NotFound();
        }

        if (!await _shoppingListService.CanManageListAsync(id, GetCurrentUserId()))
        {
            return Forbid();
        }

        var groupId = shoppingList.ShoppingGroupId;
        shoppingList = await _shoppingListService.DeleteListAsync(id, GetCurrentUserId());
        if (shoppingList is null)
        {
            return NotFound();
        }
        TempData["SuccessMessage"] = "Список удалён.";

        return RedirectToAction("Details", "ShoppingGroups", new { id = groupId });
    }

    public async Task<IActionResult> Details(int id, string filter = ShoppingItemFilter.All, string? search = null, string? category = null, string sort = ShoppingItemSortOrder.CreatedDesc)
    {
        var shoppingList = await _shoppingListService.GetUserLists(GetCurrentUserId())
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

        var viewModel = await BuildDetailsViewModelAsync(shoppingList, filter, search, category, sort);
        return View(viewModel);
    }

    private async Task<ShoppingListDetailsViewModel> BuildDetailsViewModelAsync(ShoppingList shoppingList, string filter, string? search, string? category, string sort, ShoppingItemFormViewModel? addItemForm = null)
    {
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
        addItemForm ??= new ShoppingItemFormViewModel { ShoppingListId = shoppingList.Id };
        addItemForm.ShoppingListId = shoppingList.Id;
        addItemForm.GroupMembers = groupMembers;

        return new ShoppingListDetailsViewModel
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
            AddItemForm = addItemForm
        };
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddItem([Bind(Prefix = "AddItemForm")] ShoppingItemFormViewModel model)
    {
        var shoppingList = await _shoppingListService.GetUserLists(GetCurrentUserId())
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
            var detailsList = await _shoppingListService.GetUserLists(GetCurrentUserId())
                .AsNoTracking()
                .Include(list => list.ShoppingGroup)
                .Include(list => list.Items)
                    .ThenInclude(item => item.CreatedByUser)
                .Include(list => list.Items)
                    .ThenInclude(item => item.PurchasedByUser)
                .Include(list => list.Items)
                    .ThenInclude(item => item.AssignedToUser)
                .FirstAsync(list => list.Id == model.ShoppingListId);
            return View("Details", await BuildDetailsViewModelAsync(detailsList, ShoppingItemFilter.All, null, null, ShoppingItemSortOrder.CreatedDesc, model));
        }

        var userId = GetCurrentUserId();
        var unit = ResolveUnit(model);
        var assignedUserText = await GetAssignedUserDisplayAsync(model.AssignedToUserId);
        var item = await _shoppingListService.AddItemAsync(model, userId, assignedUserText);
        if (item is null)
        {
            return NotFound();
        }
        TempData["SuccessMessage"] = "Товар добавлен.";

        return RedirectToAction(nameof(Details), new { id = model.ShoppingListId });
    }

    [HttpGet]
    public async Task<IActionResult> EditItem(int id)
    {
        var item = await _shoppingListService.GetUserItems(GetCurrentUserId())
            .AsNoTracking()
            .Include(item => item.ShoppingList)
            .Include(item => item.AssignedToUser)
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
        var item = await _shoppingListService.GetUserItems(GetCurrentUserId())
            .Include(item => item.ShoppingList)
            .Include(item => item.AssignedToUser)
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
        var oldAssignedUserText = FormatAssignedUser(item.AssignedToUser);
        var newAssignedUserText = await GetAssignedUserDisplayAsync(model.AssignedToUserId);
        if (!await _shoppingListService.UpdateItemAsync(id, model, userId, oldAssignedUserText, newAssignedUserText))
        {
            return NotFound();
        }
        TempData["SuccessMessage"] = "Изменения сохранены.";

        return RedirectToAction(nameof(Details), new { id = item.ShoppingListId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteItem(int id)
    {
        var item = await _shoppingListService.GetUserItems(GetCurrentUserId())
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
        if (await _shoppingListService.DeleteItemAsync(id, GetCurrentUserId()) is null)
        {
            return NotFound();
        }
        TempData["SuccessMessage"] = "Товар удалён.";

        return RedirectToAction(nameof(Details), new { id = listId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> TogglePurchased(int id)
    {
        var item = await _shoppingListService.GetUserItems(GetCurrentUserId())
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

        item = await _shoppingListService.TogglePurchasedAsync(id, GetCurrentUserId());
        if (item is null)
        {
            return NotFound();
        }
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

    private static string FormatItemValue(string name, decimal quantity, string? unit, string category, string? comment, string priority, decimal? estimatedPrice, string assignedUserText)
    {
        var unitText = string.IsNullOrWhiteSpace(unit) ? "без ед. изм." : unit;
        var commentText = string.IsNullOrWhiteSpace(comment) ? "без комментария" : comment;
        var priceText = estimatedPrice.HasValue ? $"{estimatedPrice.Value:N2} ₽" : "без цены";

        return $"{name}; количество: {quantity} {unitText}; категория: {category}; приоритет: {priority}; цена: {priceText}; ответственный: {assignedUserText}; комментарий: {commentText}";
    }

    private async Task<string> GetAssignedUserDisplayAsync(string? assignedUserId)
    {
        var normalizedAssignedUserId = NormalizeAssignedUserId(assignedUserId);
        if (normalizedAssignedUserId is null)
        {
            return "не назначен";
        }

        var user = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(existingUser => existingUser.Id == normalizedAssignedUserId);

        return FormatAssignedUser(user);
    }

    private static string FormatAssignedUser(ApplicationUser? user)
    {
        return user is null
            ? "не назначен"
            : user.DisplayName ?? user.Email ?? "не назначен";
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
