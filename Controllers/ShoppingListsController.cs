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
        var userId = GetCurrentUserId();
        var lists = await _context.ShoppingLists
            .AsNoTracking()
            .Include(list => list.ShoppingGroup)
            .Where(list => list.ShoppingGroup.Members.Any(member => member.ApplicationUserId == userId))
            .OrderBy(list => list.ShoppingGroup.Name)
            .ThenBy(list => list.Name)
            .ToListAsync();

        return View(lists);
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

        return RedirectToAction(nameof(Details), new { id = shoppingList.Id });
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

        return View(new ShoppingListDetailsViewModel
        {
            ShoppingList = shoppingList,
            Filter = normalizedFilter,
            Items = items.OrderBy(item => item.IsPurchased).ThenBy(item => item.Name).ToList(),
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

        if (!ModelState.IsValid)
        {
            TempData["ErrorMessage"] = "Проверьте данные товара.";
            return RedirectToAction(nameof(Details), new { id = model.ShoppingListId });
        }

        var userId = GetCurrentUserId();
        var item = new ShoppingItem
        {
            ShoppingListId = model.ShoppingListId,
            Name = model.Name,
            Quantity = model.Quantity,
            Unit = model.Unit,
            CreatedByUserId = userId,
            HistoryEntries =
            {
                new ItemHistory
                {
                    ApplicationUserId = userId,
                    Action = "Created",
                    NewValue = model.Name
                }
            }
        };

        _context.ShoppingItems.Add(item);
        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Details), new { id = model.ShoppingListId });
    }

    [HttpGet]
    public async Task<IActionResult> EditItem(int id)
    {
        var item = await GetUserItems()
            .AsNoTracking()
            .FirstOrDefaultAsync(existingItem => existingItem.Id == id);

        if (item is null)
        {
            return NotFound();
        }

        return View(new ShoppingItemFormViewModel
        {
            ShoppingListId = item.ShoppingListId,
            Name = item.Name,
            Quantity = item.Quantity,
            Unit = item.Unit
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditItem(int id, ShoppingItemFormViewModel model)
    {
        var item = await GetUserItems()
            .FirstOrDefaultAsync(existingItem => existingItem.Id == id);

        if (item is null)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            model.ShoppingListId = item.ShoppingListId;
            return View(model);
        }

        var userId = GetCurrentUserId();
        var oldValue = $"{item.Name} ({item.Quantity} {item.Unit})";

        item.Name = model.Name;
        item.Quantity = model.Quantity;
        item.Unit = model.Unit;
        item.HistoryEntries.Add(new ItemHistory
        {
            ApplicationUserId = userId,
            Action = "Updated",
            OldValue = oldValue,
            NewValue = $"{item.Name} ({item.Quantity} {item.Unit})"
        });

        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Details), new { id = item.ShoppingListId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteItem(int id)
    {
        var item = await GetUserItems()
            .FirstOrDefaultAsync(existingItem => existingItem.Id == id);

        if (item is null)
        {
            return NotFound();
        }

        var listId = item.ShoppingListId;
        _context.ShoppingItems.Remove(item);
        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Details), new { id = listId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> TogglePurchased(int id)
    {
        var item = await GetUserItems()
            .FirstOrDefaultAsync(existingItem => existingItem.Id == id);

        if (item is null)
        {
            return NotFound();
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

        return RedirectToAction(nameof(Details), new { id = item.ShoppingListId });
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
