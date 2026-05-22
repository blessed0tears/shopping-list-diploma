using Microsoft.AspNetCore.Identity;

namespace ShoppingListDiploma.Models;

public class ApplicationUser : IdentityUser
{
    public string? DisplayName { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public ICollection<GroupMember> GroupMemberships { get; set; } = new List<GroupMember>();

    public ICollection<ItemHistory> ItemHistories { get; set; } = new List<ItemHistory>();

    public ICollection<GroupInvitation> SentGroupInvitations { get; set; } = new List<GroupInvitation>();

    public ICollection<GroupInvitation> ReceivedGroupInvitations { get; set; } = new List<GroupInvitation>();

    public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
}
