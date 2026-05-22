namespace ShoppingListDiploma.Models;

public class Notification
{
    public int Id { get; set; }

    public string UserId { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;

    public string Type { get; set; } = NotificationType.General;

    public bool IsRead { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public int? RelatedGroupId { get; set; }

    public int? RelatedListId { get; set; }

    public int? RelatedItemId { get; set; }

    public int? RelatedInvitationId { get; set; }

    public ApplicationUser User { get; set; } = null!;

    public ShoppingGroup? RelatedGroup { get; set; }

    public ShoppingList? RelatedList { get; set; }

    public ShoppingItem? RelatedItem { get; set; }

    public GroupInvitation? RelatedInvitation { get; set; }
}

public static class NotificationType
{
    public const string General = "General";
    public const string GroupInvitation = "GroupInvitation";
    public const string InvitationResponse = "InvitationResponse";
    public const string NewItem = "NewItem";
    public const string ItemPurchased = "ItemPurchased";
    public const string ItemAssigned = "ItemAssigned";
    public const string ListArchived = "ListArchived";
    public const string ListRestored = "ListRestored";
}
