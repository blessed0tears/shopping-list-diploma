using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShoppingListDiploma.Data;
using ShoppingListDiploma.Models;
using ShoppingListDiploma.ViewModels;

namespace ShoppingListDiploma.Controllers;

public class HomeController : Controller
{
    private readonly ApplicationDbContext _context;

    public HomeController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        if (!User.Identity?.IsAuthenticated ?? true)
        {
            return View(new HomeViewModel());
        }

        var userId = GetCurrentUserId();
        var activeLists = await _context.ShoppingLists
            .AsNoTracking()
            .Include(list => list.ShoppingGroup)
            .Include(list => list.Items)
            .Where(list => !list.IsArchived && list.ShoppingGroup.Members.Any(member => member.ApplicationUserId == userId))
            .OrderByDescending(list => list.CreatedAtUtc)
            .Take(5)
            .ToListAsync();

        var assignedItems = await _context.ShoppingItems
            .AsNoTracking()
            .Include(item => item.ShoppingList)
            .Where(item => item.AssignedToUserId == userId)
            .OrderByDescending(item => item.CreatedAtUtc)
            .Take(5)
            .ToListAsync();

        var invitations = await _context.GroupInvitations
            .AsNoTracking()
            .Include(item => item.ShoppingGroup)
            .Include(item => item.InvitedByUser)
            .Where(item => item.InvitedUserId == userId && item.Status == GroupInvitationStatus.Pending)
            .OrderByDescending(item => item.CreatedAtUtc)
            .ToListAsync();

        var notifications = await _context.Notifications
            .AsNoTracking()
            .Where(item => item.UserId == userId)
            .OrderByDescending(item => item.CreatedAtUtc)
            .Take(5)
            .ToListAsync();

        return View(new HomeViewModel
        {
            IsAuthenticated = true,
            UserEmail = User.Identity?.Name,
            ActiveLists = activeLists,
            AssignedItems = assignedItems,
            PendingInvitations = invitations,
            LatestNotifications = notifications
        });
    }

    public IActionResult Privacy() => View();

    public IActionResult Guide() => View();

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel
        {
            RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
        });
    }

    private string GetCurrentUserId()
    {
        return User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new InvalidOperationException("Current user id is not available.");
    }
}
