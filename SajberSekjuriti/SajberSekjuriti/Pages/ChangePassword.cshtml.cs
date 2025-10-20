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
        private readonly AuditLogService _auditLogService;
        // Konstruktor klasy ChangePasswordModel z wstrzykiwaniem zale¿noœci
        public ChangePasswordModel(AuditLogService auditLogService,UserService userService, PasswordService passwordService, PasswordPolicyService policyService, PasswordValidationService validationService)
        {
            _userService = userService;
            _passwordService = passwordService;
            _policyService = policyService;
            _validationService = validationService;
            _auditLogService = auditLogService;
        }
        // Model powi¹zany z formularzem zmiany has³a
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
        // Obs³uga ¿¹dania POST do zmiany has³a
        public async Task<IActionResult> OnPostAsync()
        {
            //Sprawdzenie poprawnoœci modelu
            if (!ModelState.IsValid)
            {
                return Page();
            }
            // Pobranie polityki hase³ i walidacja nowego has³a
            var policy = await _policyService.GetSettingsAsync();
            var validationError = _validationService.Validate(Input.NewPassword, policy);
            if (validationError != null)
            {
                ModelState.AddModelError(string.Empty, validationError);
                return Page();
            }
            // Pobranie u¿ytkownika i zmiana has³a
            var username = User.Identity?.Name;
            if (username == null)
            {
                return RedirectToPage("/Login");
            }
            var user = await _userService.GetByUsernameAsync(username);
            if (user == null || !_passwordService.VerifyPassword(Input.OldPassword, user.PasswordHash))
            {
                ModelState.AddModelError("Input.OldPassword", "Stare has³o jest nieprawid³owe.");
                return Page();
            }
            // Sprawdzenie historii hase³

            foreach (var oldHash in user.PasswordHistory)
            {
                if (_passwordService.VerifyPassword(Input.NewPassword, oldHash))
                {
                    ModelState.AddModelError("Input.NewPassword", "Nowe has³o nie mo¿e byæ takie samo jak jedno z 5 poprzednich.");
                    return Page();
                }
            }
            // Aktualizacja has³a i historii hase³
            user.PasswordHistory.Add(user.PasswordHash);

            // Utrzymanie tylko ostatnich 5 hase³ w historii
            while (user.PasswordHistory.Count > 5)
            {
                user.PasswordHistory.RemoveAt(0);
            }
            // Ustawienie nowego has³a
            user.PasswordHash = _passwordService.HashPassword(Input.NewPassword);
            user.MustChangePassword = false;
            user.PasswordLastSet = DateTime.UtcNow;
            // Zapisanie zmian w bazie danych
            await _userService.UpdateAsync(user);
            await _auditLogService.LogAsync(username, "Zmiana has³a", "U¿ytkownik zosta³ zmuszony do zmiany has³a.");

            return RedirectToPage("/Index");
        }
    }
}