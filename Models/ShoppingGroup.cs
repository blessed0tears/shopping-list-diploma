namespace ShoppingListDiploma.Models;

public class ShoppingGroup
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string? OwnerId { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public ApplicationUser? Owner { get; set; }

    public ICollection<GroupMember> Members { get; set; } = new List<GroupMember>();

    public ICollection<ShoppingList> ShoppingLists { get; set; } = new List<ShoppingList>();
}
