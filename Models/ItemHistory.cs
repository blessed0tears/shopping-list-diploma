namespace ShoppingListDiploma.Models;

public class ItemHistory
{
    public int Id { get; set; }

    public int ShoppingItemId { get; set; }

    public string? ApplicationUserId { get; set; }

    public string Action { get; set; } = string.Empty;

    public string? OldValue { get; set; }

    public string? NewValue { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public ShoppingItem ShoppingItem { get; set; } = null!;

    public ApplicationUser? ApplicationUser { get; set; }
}
