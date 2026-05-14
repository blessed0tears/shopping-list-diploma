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

        return View(new ShoppingGroupDetailsViewModel
        {
            Group = group,
            Members = group.Members.OrderBy(member => member.ApplicationUser.Email).ToList(),
            ShoppingLists = group.ShoppingLists.OrderBy(list => list.Name).ToList()
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddMember(int id, AddGroupMemberViewModel model)
    {
        var group = await GetUserGroupQuery()
            .Include(group => group.Members)
            .FirstOrDefaultAsync(group => group.Id == id);

        if (group is null)
        {
            return NotFound();
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
        TempData["SuccessMessage"] = "Участник добавлен в группу.";

        return RedirectToAction(nameof(Details), new { id });
    }

    private IQueryable<ShoppingGroup> GetUserGroupQuery()
    {
        var userId = GetCurrentUserId();

        return _context.ShoppingGroups
            .Where(group => group.Members.Any(member => member.ApplicationUserId == userId));
    }

    private string GetCurrentUserId()
    {
        return User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new InvalidOperationException("Current user id is not available.");
    }
}
