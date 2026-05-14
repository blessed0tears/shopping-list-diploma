using System.ComponentModel.DataAnnotations;
using ShoppingListDiploma.Models;

namespace ShoppingListDiploma.ViewModels;

public class ShoppingListsIndexViewModel
{
    public IReadOnlyCollection<ShoppingList> ActiveLists { get; set; } = Array.Empty<ShoppingList>();

    public IReadOnlyCollection<ShoppingList> ArchivedLists { get; set; } = Array.Empty<ShoppingList>();
}

public class CreateShoppingListViewModel
{
    public int ShoppingGroupId { get; set; }

    [Required(ErrorMessage = "Введите название списка.")]
    [StringLength(150, ErrorMessage = "Название списка не должно превышать {1} символов.")]
    [Display(Name = "Название списка")]
    public string Name { get; set; } = string.Empty;
}

public class EditShoppingListViewModel
{
    public int Id { get; set; }

    public int ShoppingGroupId { get; set; }

    [Required(ErrorMessage = "Введите название списка.")]
    [StringLength(150, ErrorMessage = "Название списка не должно превышать {1} символов.")]
    [Display(Name = "Название списка")]
    public string Name { get; set; } = string.Empty;

    [Display(Name = "Архивный список")]
    public bool IsArchived { get; set; }
}

public class ShoppingListDetailsViewModel
{
    public ShoppingList ShoppingList { get; set; } = null!;

    public string Filter { get; set; } = ShoppingItemFilter.All;

    public IReadOnlyCollection<ShoppingItem> Items { get; set; } = Array.Empty<ShoppingItem>();

    public IReadOnlyCollection<ItemHistory> HistoryEntries { get; set; } = Array.Empty<ItemHistory>();

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

    [StringLength(50, ErrorMessage = "Своя единица измерения не должна превышать {1} символов.")]
    [Display(Name = "Своя единица измерения")]
    public string? CustomUnit { get; set; }

    [Required(ErrorMessage = "Выберите категорию товара.")]
    [StringLength(100, ErrorMessage = "Категория не должна превышать {1} символов.")]
    [Display(Name = "Категория")]
    public string Category { get; set; } = ShoppingItemCategory.Other;

    [StringLength(500, ErrorMessage = "Комментарий не должен превышать {1} символов.")]
    [Display(Name = "Комментарий")]
    public string? Comment { get; set; }

    public static IReadOnlyList<string> UnitOptions { get; } = new[]
    {
        "шт.",
        "кг",
        "г",
        "л",
        "мл",
        "пачка",
        "бутылка",
        "упаковка",
        "банка",
        OtherUnitValue
    };

    public static IReadOnlyList<string> CategoryOptions { get; } = new[]
    {
        ShoppingItemCategory.Products,
        ShoppingItemCategory.FruitsAndVegetables,
        ShoppingItemCategory.Dairy,
        ShoppingItemCategory.MeatAndFish,
        ShoppingItemCategory.Drinks,
        ShoppingItemCategory.Cleaning,
        ShoppingItemCategory.HomeGoods,
        ShoppingItemCategory.Other
    };

    public const string OtherUnitValue = "другое";
}

public static class ShoppingItemCategory
{
    public const string Products = "Продукты";

    public const string FruitsAndVegetables = "Овощи и фрукты";

    public const string Dairy = "Молочные продукты";

    public const string MeatAndFish = "Мясо и рыба";

    public const string Drinks = "Напитки";

    public const string Cleaning = "Бытовая химия";

    public const string HomeGoods = "Товары для дома";

    public const string Other = "Другое";
}

public static class ShoppingItemFilter
{
    public const string All = "all";

    public const string Purchased = "purchased";

    public const string Active = "active";
}
