using System.ComponentModel.DataAnnotations;

namespace ShoppingListDiploma.ViewModels;

public class RegisterViewModel
{
    [Required(ErrorMessage = "Введите имя пользователя.")]
    [StringLength(100, ErrorMessage = "Имя пользователя не должно превышать {1} символов.")]
    [Display(Name = "Имя пользователя")]
    public string DisplayName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Введите email.")]
    [EmailAddress(ErrorMessage = "Введите корректный email.")]
    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Введите пароль.")]
    [StringLength(100, ErrorMessage = "Пароль должен содержать от {2} до {1} символов.", MinimumLength = 6)]
    [DataType(DataType.Password)]
    [Display(Name = "Пароль")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Подтвердите пароль.")]
    [DataType(DataType.Password)]
    [Display(Name = "Подтверждение пароля")]
    [Compare(nameof(Password), ErrorMessage = "Пароль и подтверждение пароля не совпадают.")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
