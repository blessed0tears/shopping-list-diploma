namespace ShoppingListDiploma.Models;

public class GroupMember
{
    public int Id { get; set; }

    public int ShoppingGroupId { get; set; }

    public string ApplicationUserId { get; set; } = string.Empty;

    public string Role { get; set; } = "Member";

    public DateTime JoinedAtUtc { get; set; } = DateTime.UtcNow;

    public ShoppingGroup ShoppingGroup { get; set; } = null!;

    public ApplicationUser ApplicationUser { get; set; } = null!;
}
