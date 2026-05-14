using System.ComponentModel.DataAnnotations;
using ShoppingListDiploma.Models;

namespace ShoppingListDiploma.ViewModels;

public class ShoppingGroupIndexViewModel
{
    public IReadOnlyCollection<ShoppingGroup> Groups { get; set; } = Array.Empty<ShoppingGroup>();
}

public class CreateShoppingGroupViewModel
{
    [Required(ErrorMessage = "Введите название группы.")]
    [StringLength(150, ErrorMessage = "Название группы не должно превышать {1} символов.")]
    [Display(Name = "Название группы")]
    public string Name { get; set; } = string.Empty;

    [StringLength(500, ErrorMessage = "Описание не должно превышать {1} символов.")]
    [Display(Name = "Описание")]
    public string? Description { get; set; }
}

public class EditShoppingGroupViewModel : CreateShoppingGroupViewModel
{
    public int Id { get; set; }
}

public class AddGroupMemberViewModel
{
    [Required(ErrorMessage = "Введите email участника.")]
    [EmailAddress(ErrorMessage = "Введите корректный email.")]
    [Display(Name = "Email участника")]
    public string Email { get; set; } = string.Empty;
}

public class ShoppingGroupDetailsViewModel
{
    public ShoppingGroup Group { get; set; } = null!;

    public IReadOnlyCollection<GroupMember> Members { get; set; } = Array.Empty<GroupMember>();

    public IReadOnlyCollection<ShoppingList> ActiveShoppingLists { get; set; } = Array.Empty<ShoppingList>();

    public IReadOnlyCollection<ShoppingList> ArchivedShoppingLists { get; set; } = Array.Empty<ShoppingList>();

    public AddGroupMemberViewModel AddMemberForm { get; set; } = new();

    public bool IsCurrentUserOwner { get; set; }
}
