using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SajberSekjuriti.Services;
using SajberSekjuriti.Services;
using System.ComponentModel.DataAnnotations;

namespace SajberSekjuriti.Pages
{
    [Authorize]
    public class ChangePasswordModel : PageModel
    {
        private readonly UserService _userService;
        private readonly PasswordService _passwordService;
        private readonly PasswordPolicyService _policyService;
        private readonly PasswordValidationService _validationService;

        public ChangePasswordModel(UserService userService, PasswordService passwordService, PasswordPolicyService policyService, PasswordValidationService validationService)
        {
            _userService = userService;
            _passwordService = passwordService;
            _policyService = policyService;
            _validationService = validationService;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public string? ErrorMessage { get; set; }

        public class InputModel
        {
            [Required(ErrorMessage = "Stare has³o jest wymagane")]
            [DataType(DataType.Password)]
            [Display(Name = "Stare has³o")]
            public string OldPassword { get; set; } = string.Empty;

            [Required(ErrorMessage = "Nowe has³o jest wymagane")]
            [StringLength(100, ErrorMessage = "Has³o musi mieæ co najmniej 6 znaków.", MinimumLength = 6)]
            [DataType(DataType.Password)]
            [Display(Name = "Nowe has³o")]
            public string NewPassword { get; set; } = string.Empty;

            [DataType(DataType.Password)]
            [Display(Name = "PotwierdŸ nowe has³o")]
            [Compare("NewPassword", ErrorMessage = "Has³a nie s¹ takie same.")]
            public string ConfirmPassword { get; set; } = string.Empty;
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var policy = await _policyService.GetSettingsAsync();
            var validationError = _validationService.Validate(Input.NewPassword, policy);
            if (validationError != null)
            {
                ModelState.AddModelError(string.Empty, validationError);
                return Page();
            }

            var username = User.Identity?.Name;
            if (username == null)
            {
                // To siê nie powinno zdarzyæ, bo strona jest pod [Authorize]
                return RedirectToPage("/Login");
            }

            var user = await _userService.GetByUsernameAsync(username);
            if (user == null || !_passwordService.VerifyPassword(Input.OldPassword, user.PasswordHash))
            {
                ErrorMessage = "Stare has³o jest nieprawid³owe.";
                return Page();
            }

            user.PasswordHash = _passwordService.HashPassword(Input.NewPassword);
            user.MustChangePassword = false;
            user.PasswordLastSet = DateTime.UtcNow;

            await _userService.UpdateAsync(user);

            return RedirectToPage("/Index");
        }
    }
}