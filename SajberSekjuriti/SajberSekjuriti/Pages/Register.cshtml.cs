using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SajberSekjuriti.Model;
using SajberSekjuriti.Model;
using SajberSekjuriti.Services;
using System.ComponentModel.DataAnnotations;

namespace SajberSekjuriti.Pages
{
    public class RegisterModel : PageModel
    {
        private readonly UserService _userService;
        private readonly PasswordService _passwordService;
        private readonly PasswordPolicyService _policyService;
        private readonly PasswordValidationService _validationService;

        public RegisterModel(UserService userService, PasswordService passwordService, PasswordPolicyService policyService, PasswordValidationService validationService)
        {
            _userService = userService;
            _passwordService = passwordService;
            _policyService = policyService;
            _validationService = validationService;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public class InputModel
        {
            [Required(ErrorMessage = "Login jest wymagany")]
            [Display(Name = "Login")]
            public string Username { get; set; } = string.Empty;

            [Required(ErrorMessage = "Pe³na nazwa jest wymagana")]
            [Display(Name = "Pe³na nazwa")]
            public string FullName { get; set; } = string.Empty;

            [Required(ErrorMessage = "Has³o jest wymagane")]
            [StringLength(100, ErrorMessage = "Has³o musi mieæ co najmniej 6 znaków.", MinimumLength = 6)]
            [DataType(DataType.Password)]
            [Display(Name = "Has³o")]
            public string Password { get; set; } = string.Empty;

            [DataType(DataType.Password)]
            [Display(Name = "PotwierdŸ has³o")]
            [Compare("Password", ErrorMessage = "Has³a nie s¹ takie same.")]
            public string ConfirmPassword { get; set; } = string.Empty;
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }
            var policy = await _policyService.GetSettingsAsync();
            var validationError = _validationService.Validate(Input.Password, policy);
            if (validationError != null)
            {
                ModelState.AddModelError(string.Empty, validationError);
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
                Role = UserRole.User,
                PasswordLastSet = DateTime.UtcNow
            };

            await _userService.CreateAsync(newUser);

            TempData["SuccessMessage"] = "Konto zosta³o pomyœlnie utworzone! Mo¿esz siê teraz zalogowaæ.";

            return RedirectToPage("/Login");
        }
    }
}