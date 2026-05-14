using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShoppingListDiploma.Data;
using ShoppingListDiploma.Models;
using ShoppingListDiploma.ViewModels;

namespace ShoppingListDiploma.Controllers;

[Authorize]
public class ShoppingGroupsController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public ShoppingGroupsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index()
    {
        var userId = GetCurrentUserId();
        var groups = await _context.ShoppingGroups
            .AsNoTracking()
            .Where(group => group.Members.Any(member => member.ApplicationUserId == userId))
            .Include(group => group.Owner)
            .Include(group => group.Members)
            .OrderBy(group => group.Name)
            .ToListAsync();

        return View(new ShoppingGroupIndexViewModel
        {
            Groups = groups
        });
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View(new CreateShoppingGroupViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateShoppingGroupViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var userId = GetCurrentUserId();
        var group = new ShoppingGroup
        {
            Name = model.Name,
            Description = model.Description,
            OwnerId = userId,
            Members =
            {
                new GroupMember
                {
                    ApplicationUserId = userId,
                    Role = "Owner"
                }
            }
        };

        _context.ShoppingGroups.Add(group);
        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = "Группа создана.";

        return RedirectToAction(nameof(Details), new { id = group.Id });
    }

    public async Task<IActionResult> Details(int id)
    {
        var group = await GetUserGroupQuery()
            .Include(group => group.Owner)
            .Include(group => group.Members)
                .ThenInclude(member => member.ApplicationUser)
            .Include(group => group.ShoppingLists)
            .FirstOrDefaultAsync(group => group.Id == id);

        if (group is null)
        {
            return NotFound();
        }

        var userId = GetCurrentUserId();
        var isOwner = IsOwner(group, userId);
        var activeLists = group.ShoppingLists
            .Where(list => !list.IsArchived)
            .OrderBy(list => list.Name)
            .ToList();
        var archivedLists = group.ShoppingLists
            .Where(list => list.IsArchived)
            .OrderBy(list => list.Name)
            .ToList();

        return View(new ShoppingGroupDetailsViewModel
        {
            Group = group,
            Members = group.Members.OrderBy(member => member.ApplicationUser.Email).ToList(),
            ActiveShoppingLists = activeLists,
            ArchivedShoppingLists = archivedLists,
            IsCurrentUserOwner = isOwner
        });
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var group = await GetOwnedGroupQuery()
            .AsNoTracking()
            .FirstOrDefaultAsync(group => group.Id == id);

        if (group is null)
        {
            return Forbid();
        }

        return View(new EditShoppingGroupViewModel
        {
            Id = group.Id,
            Name = group.Name,
            Description = group.Description
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, EditShoppingGroupViewModel model)
    {
        if (id != model.Id)
        {
            return BadRequest();
        }

        var group = await GetOwnedGroupQuery()
            .FirstOrDefaultAsync(group => group.Id == id);

        if (group is null)
        {
            return Forbid();
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        group.Name = model.Name;
        group.Description = model.Description;
        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = "Изменения сохранены.";

        return RedirectToAction(nameof(Details), new { id = group.Id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddMember(int id, AddGroupMemberViewModel model)
    {
        var group = await GetOwnedGroupQuery()
            .Include(group => group.Members)
            .FirstOrDefaultAsync(group => group.Id == id);

        if (group is null)
        {
            return Forbid();
        }

        if (!ModelState.IsValid)
        {
            TempData["ErrorMessage"] = "Введите корректный email участника.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var normalizedEmail = model.Email.Trim();
        var user = await _userManager.FindByEmailAsync(normalizedEmail);
        if (user is null)
        {
            TempData["ErrorMessage"] = "Пользователь с таким email не найден.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var isAlreadyMember = group.Members.Any(member => member.ApplicationUserId == user.Id);
        if (isAlreadyMember)
        {
            TempData["ErrorMessage"] = "Пользователь уже состоит в этой группе.";
            return RedirectToAction(nameof(Details), new { id });
        }

        group.Members.Add(new GroupMember
        {
            ApplicationUserId = user.Id,
            Role = "Member"
        });

        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = "Участник добавлен.";

        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveMember(int id, int memberId)
    {
        var group = await GetOwnedGroupQuery()
            .Include(group => group.Members)
            .FirstOrDefaultAsync(group => group.Id == id);

        if (group is null)
        {
            return Forbid();
        }

        var member = group.Members.FirstOrDefault(member => member.Id == memberId);
        if (member is null)
        {
            return NotFound();
        }

        if (member.ApplicationUserId == group.OwnerId || member.Role == "Owner")
        {
            TempData["ErrorMessage"] = "Нельзя удалить владельца группы.";
            return RedirectToAction(nameof(Details), new { id });
        }

        _context.GroupMembers.Remove(member);
        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = "Участник удалён.";

        return RedirectToAction(nameof(Details), new { id });
    }

    private IQueryable<ShoppingGroup> GetUserGroupQuery()
    {
        var userId = GetCurrentUserId();

        return _context.ShoppingGroups
            .Where(group => group.Members.Any(member => member.ApplicationUserId == userId));
    }

    private IQueryable<ShoppingGroup> GetOwnedGroupQuery()
    {
        var userId = GetCurrentUserId();

        return _context.ShoppingGroups
            .Where(group => group.OwnerId == userId
                || group.Members.Any(member => member.ApplicationUserId == userId && member.Role == "Owner"));
    }

    private string GetCurrentUserId()
    {
        return User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new InvalidOperationException("Current user id is not available.");
    }

    private static bool IsOwner(ShoppingGroup group, string userId)
    {
        return group.OwnerId == userId
            || group.Members.Any(member => member.ApplicationUserId == userId && member.Role == "Owner");
    }
}
