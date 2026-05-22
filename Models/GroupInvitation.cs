namespace ShoppingListDiploma.Models;

public class GroupInvitation
{
    public int Id { get; set; }

    public int ShoppingGroupId { get; set; }

    public string InvitedUserId { get; set; } = string.Empty;

    public string InvitedByUserId { get; set; } = string.Empty;

    public string Status { get; set; } = GroupInvitationStatus.Pending;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime? RespondedAtUtc { get; set; }

    public ShoppingGroup ShoppingGroup { get; set; } = null!;

    public ApplicationUser InvitedUser { get; set; } = null!;

    public ApplicationUser InvitedByUser { get; set; } = null!;

    public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
}

public static class GroupInvitationStatus
{
    public const string Pending = "Pending";

    public const string Accepted = "Accepted";

    public const string Declined = "Declined";
}
