using System.ComponentModel.DataAnnotations;
using ShoppingListDiploma.Models;

namespace ShoppingListDiploma.ViewModels;

public class CreateShoppingListViewModel
{
    public int ShoppingGroupId { get; set; }

    [Required(ErrorMessage = "Введите название списка.")]
    [StringLength(150, ErrorMessage = "Название списка не должно превышать {1} символов.")]
    [Display(Name = "Название списка")]
    public string Name { get; set; } = string.Empty;
}

public class ShoppingListDetailsViewModel
{
    public ShoppingList ShoppingList { get; set; } = null!;

    public string Filter { get; set; } = ShoppingItemFilter.All;

    public IReadOnlyCollection<ShoppingItem> Items { get; set; } = Array.Empty<ShoppingItem>();

    public ShoppingItemFormViewModel AddItemForm { get; set; } = new();
}

public class ShoppingItemFormViewModel
{
    public int ShoppingListId { get; set; }

    [Required(ErrorMessage = "Введите название товара.")]
    [StringLength(200, ErrorMessage = "Название товара не должно превышать {1} символов.")]
    [Display(Name = "Название товара")]
    public string Name { get; set; } = string.Empty;

    [Range(0.01, 999999, ErrorMessage = "Количество должно быть больше 0.")]
    [Display(Name = "Количество")]
    public decimal Quantity { get; set; } = 1;

    [StringLength(50, ErrorMessage = "Единица измерения не должна превышать {1} символов.")]
    [Display(Name = "Единица измерения")]
    public string? Unit { get; set; }
}

public static class ShoppingItemFilter
{
    public const string All = "all";

    public const string Purchased = "purchased";

    public const string Active = "active";
}
