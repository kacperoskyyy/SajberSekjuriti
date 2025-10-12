using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SajberSekjuriti.Model;
using SajberSekjuriti.Services;
using System.ComponentModel.DataAnnotations;

namespace SajberSekjuriti.Pages
{
    [Authorize(Roles = "Admin")]
    public class AddUserModel : PageModel
    {
        private readonly UserService _userService;
        private readonly PasswordService _passwordService;
        // Konstruktor klasy dodaj¹cy serwisy UserService i PasswordService
        public AddUserModel(UserService userService, PasswordService passwordService)
        {
            _userService = userService;
            _passwordService = passwordService;
        }
        // Model powi¹zany z formularzem dodawania u¿ytkownika
        [BindProperty]
        public InputModel Input { get; set; } = new();

        public class InputModel
        {
            [Required(ErrorMessage = "Login jest wymagany")]
            public string Username { get; set; } = string.Empty;

            [Required(ErrorMessage = "Pe³na nazwa jest wymagana")]
            [Display(Name = "Pe³na nazwa")]
            public string FullName { get; set; } = string.Empty;

            [Required(ErrorMessage = "Has³o jest wymagane")]
            [StringLength(100, ErrorMessage = "Has³o musi mieæ co najmniej 6 znaków.", MinimumLength = 6)]
            [DataType(DataType.Password)]
            [Display(Name = "Has³o")]
            public string Password { get; set; } = string.Empty;
            [Required]
            [Display(Name = "Rola")]
            public UserRole Role { get; set; }
        }
        // Metoda obs³uguj¹ca ¿¹danie POST z formularza dodawania u¿ytkownika
        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var existingUser = await _userService.GetByUsernameAsync(Input.Username);
            if (existingUser != null)
            {
                ModelState.AddModelError(string.Empty, "U¿ytkownik o takim loginie ju¿ istnieje.");
                return Page();
            }

            var newUser = new User
            {
                Username = Input.Username,
                FullName = Input.FullName,
                PasswordHash = _passwordService.HashPassword(Input.Password),
                Role = Input.Role,
                PasswordLastSet = DateTime.UtcNow
            };

            await _userService.CreateAsync(newUser);

            return RedirectToPage("/AdminPanel");
        }
    }
}