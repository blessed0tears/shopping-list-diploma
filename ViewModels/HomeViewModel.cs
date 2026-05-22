using ShoppingListDiploma.Models;

namespace ShoppingListDiploma.ViewModels;

public class HomeViewModel
{
    public string Title { get; set; } = "Совместный список покупок";

    public string Description { get; set; } =
        "Информационная система для совместного планирования покупок.";

    public bool IsAuthenticated { get; set; }

    public string? UserEmail { get; set; }

    public IReadOnlyCollection<ShoppingList> ActiveLists { get; set; } = Array.Empty<ShoppingList>();

    public IReadOnlyCollection<ShoppingItem> AssignedItems { get; set; } = Array.Empty<ShoppingItem>();

    public IReadOnlyCollection<GroupInvitation> PendingInvitations { get; set; } = Array.Empty<GroupInvitation>();

    public IReadOnlyCollection<Notification> LatestNotifications { get; set; } = Array.Empty<Notification>();
}
