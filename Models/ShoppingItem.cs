namespace ShoppingListDiploma.Models;

public class ShoppingItem
{
    public int Id { get; set; }

    public int ShoppingListId { get; set; }

    public string Name { get; set; } = string.Empty;

    public decimal Quantity { get; set; } = 1;

    public string? Unit { get; set; }

    public string Category { get; set; } = "Другое";

    public string? Comment { get; set; }

    public bool IsPurchased { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime? PurchasedAtUtc { get; set; }

    public string? CreatedByUserId { get; set; }

    public string? PurchasedByUserId { get; set; }

    public ShoppingList ShoppingList { get; set; } = null!;

    public ApplicationUser? CreatedByUser { get; set; }

    public ApplicationUser? PurchasedByUser { get; set; }

    public ICollection<ItemHistory> HistoryEntries { get; set; } = new List<ItemHistory>();
}
