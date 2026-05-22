using ShoppingListDiploma.Models;

namespace ShoppingListDiploma.ViewModels;

public class NotificationsIndexViewModel
{
    public IReadOnlyCollection<Notification> Notifications { get; set; } = Array.Empty<Notification>();
}
