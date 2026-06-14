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
    [StringLength(150, ErrorMessage = "Название списка не должно превышать {1} символов. Сократите название и попробуйте снова.")]
    [Display(Name = "Название списка")]
    public string Name { get; set; } = string.Empty;
}

public class EditShoppingListViewModel
{
    public int Id { get; set; }

    public int ShoppingGroupId { get; set; }

    [Required(ErrorMessage = "Введите название списка.")]
    [StringLength(150, ErrorMessage = "Название списка не должно превышать {1} символов. Сократите название и попробуйте снова.")]
    [Display(Name = "Название списка")]
    public string Name { get; set; } = string.Empty;

    [Display(Name = "Архивный список")]
    public bool IsArchived { get; set; }
}

public class ShoppingListDetailsViewModel
{
    public ShoppingList ShoppingList { get; set; } = null!;

    public string Filter { get; set; } = ShoppingItemFilter.All;

    public string? SearchQuery { get; set; }

    public string? CategoryFilter { get; set; }

    public string SortOrder { get; set; } = ShoppingItemSortOrder.CreatedDesc;

    public decimal TotalEstimatedPrice { get; set; }

    public decimal PurchasedEstimatedPrice { get; set; }

    public decimal ActiveEstimatedPrice { get; set; }

    public int TotalItemCount { get; set; }

    public bool HasActiveSearchOrFilters => !string.IsNullOrWhiteSpace(SearchQuery)
        || !string.IsNullOrWhiteSpace(CategoryFilter)
        || Filter != ShoppingItemFilter.All;

    public IReadOnlyCollection<ShoppingItem> Items { get; set; } = Array.Empty<ShoppingItem>();

    public IReadOnlyCollection<GroupMember> GroupMembers { get; set; } = Array.Empty<GroupMember>();

    public IReadOnlyCollection<ItemHistory> HistoryEntries { get; set; } = Array.Empty<ItemHistory>();

    public ShoppingItemFormViewModel AddItemForm { get; set; } = new();
}

public class ShoppingItemFormViewModel
{
    public int ShoppingListId { get; set; }

    [Required(ErrorMessage = "Введите название товара.")]
    [StringLength(200, ErrorMessage = "Название товара не должно превышать {1} символов. Сократите название и попробуйте снова.")]
    [Display(Name = "Название товара")]
    public string Name { get; set; } = string.Empty;

    [Range(0.01, 999999, ErrorMessage = "Укажите количество больше 0.")]
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

    [Required(ErrorMessage = "Выберите приоритет товара.")]
    [StringLength(50, ErrorMessage = "Приоритет не должен превышать {1} символов.")]
    [Display(Name = "Приоритет")]
    public string Priority { get; set; } = ShoppingItemPriority.Normal;

    [Range(0.01, 9999999, ErrorMessage = "Укажите цену больше 0 или оставьте поле пустым.")]
    [Display(Name = "Примерная цена")]
    public decimal? EstimatedPrice { get; set; }

    [Display(Name = "Ответственный")]
    public string? AssignedToUserId { get; set; }

    public IReadOnlyCollection<GroupMember> GroupMembers { get; set; } = Array.Empty<GroupMember>();

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

    public static IReadOnlyList<string> PriorityOptions { get; } = new[]
    {
        ShoppingItemPriority.Low,
        ShoppingItemPriority.Normal,
        ShoppingItemPriority.High
    };

    public const string OtherUnitValue = "другое";
}

public static class ShoppingItemPriority
{
    public const string Low = "Низкий";

    public const string Normal = "Обычный";

    public const string High = "Высокий";
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

public static class ShoppingItemSortOrder
{
    public const string CreatedDesc = "created_desc";

    public const string CreatedAsc = "created_asc";

    public const string NameAsc = "name_asc";

    public const string NameDesc = "name_desc";
}
