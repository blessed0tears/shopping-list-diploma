namespace ShoppingListDiploma.Models;

public class ShoppingList
{
    public int Id { get; set; }

    public int ShoppingGroupId { get; set; }

    public string Name { get; set; } = string.Empty;

    public bool IsArchived { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public ShoppingGroup ShoppingGroup { get; set; } = null!;

    public ICollection<ShoppingItem> Items { get; set; } = new List<ShoppingItem>();
}
