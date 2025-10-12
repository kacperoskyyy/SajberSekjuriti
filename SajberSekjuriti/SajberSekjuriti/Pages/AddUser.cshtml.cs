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
        // Konstruktor klasy dodaj�cy serwisy UserService i PasswordService
        public AddUserModel(UserService userService, PasswordService passwordService)
        {
            _userService = userService;
            _passwordService = passwordService;
        }
        // Model powi�zany z formularzem dodawania u�ytkownika
        [BindProperty]
        public InputModel Input { get; set; } = new();

        public class InputModel
        {
            [Required(ErrorMessage = "Login jest wymagany")]
            public string Username { get; set; } = string.Empty;

            [Required(ErrorMessage = "Pe�na nazwa jest wymagana")]
            [Display(Name = "Pe�na nazwa")]
            public string FullName { get; set; } = string.Empty;

            [Required(ErrorMessage = "Has�o jest wymagane")]
            [StringLength(100, ErrorMessage = "Has�o musi mie� co najmniej 6 znak�w.", MinimumLength = 6)]
            [DataType(DataType.Password)]
            [Display(Name = "Has�o")]
            public string Password { get; set; } = string.Empty;
            [Required]
            [Display(Name = "Rola")]
            public UserRole Role { get; set; }
        }
        // Metoda obs�uguj�ca ��danie POST z formularza dodawania u�ytkownika
        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var existingUser = await _userService.GetByUsernameAsync(Input.Username);
            if (existingUser != null)
            {
                ModelState.AddModelError(string.Empty, "U�ytkownik o takim loginie ju� istnieje.");
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