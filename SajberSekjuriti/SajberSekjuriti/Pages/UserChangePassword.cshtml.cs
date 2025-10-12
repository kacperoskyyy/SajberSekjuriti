using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SajberSekjuriti.Model;
using SajberSekjuriti.Services;
using System.ComponentModel.DataAnnotations;

namespace SajberSekjuriti.Pages
{
    [Authorize]
    public class UserChangePasswordModel : PageModel
    {
        private readonly UserService _userService;
        private readonly PasswordService _passwordService;
        private readonly PasswordPolicyService _policyService;
        private readonly PasswordValidationService _validationService;

        public UserChangePasswordModel(UserService userService, PasswordService passwordService, PasswordPolicyService policyService, PasswordValidationService validationService)
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
            [Required(ErrorMessage = "Stare has≥o jest wymagane")]
            [DataType(DataType.Password)]
            [Display(Name = "Stare has≥o")]
            public string OldPassword { get; set; } = string.Empty;

            [Required(ErrorMessage = "Nowe has≥o jest wymagane")]
            [DataType(DataType.Password)]
            [Display(Name = "Nowe has≥o")]
            public string NewPassword { get; set; } = string.Empty;

            [DataType(DataType.Password)]
            [Display(Name = "Potwierdü nowe has≥o")]
            [Compare("NewPassword", ErrorMessage = "Has≥a nie sπ takie same.")]
            public string ConfirmPassword { get; set; } = string.Empty;
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var username = User.Identity?.Name;
            if (string.IsNullOrEmpty(username))
            {
                return RedirectToPage("/Login");
            }

            var policy = await _policyService.GetSettingsAsync();
            var validationError = _validationService.Validate(Input.NewPassword, policy);
            if (validationError != null)
            {
                ModelState.AddModelError(string.Empty, validationError);
                return Page();
            }

            var user = await _userService.GetByUsernameAsync(username);
            if (user == null || !_passwordService.VerifyPassword(Input.OldPassword, user.PasswordHash))
            {
                ModelState.AddModelError("Input.OldPassword", "Stare has≥o jest nieprawid≥owe.");
                return Page();
            }

            //SPRAWDZANIE HISTORII HAS£a
            foreach (var oldHash in user.PasswordHistory)
            {
                if (_passwordService.VerifyPassword(Input.NewPassword, oldHash))
                {
                    ModelState.AddModelError("Input.NewPassword", "Nowe has≥o nie moøe byÊ takie samo jak jedno z 5 poprzednich.");
                    return Page();
                }
            }
            user.PasswordHistory.Add(user.PasswordHash);

            while (user.PasswordHistory.Count > 5)
            {
                user.PasswordHistory.RemoveAt(0);
            }

            user.PasswordHash = _passwordService.HashPassword(Input.NewPassword);
            user.PasswordLastSet = DateTime.UtcNow;
            user.MustChangePassword = false;

            await _userService.UpdateAsync(user);

            TempData["SuccessMessage"] = "Has≥o zosta≥o pomyúlnie zmienione.";
            return RedirectToPage("/Index");
        }
    }
}