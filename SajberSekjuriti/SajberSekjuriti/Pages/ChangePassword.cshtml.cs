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
        // Konstruktor klasy ChangePasswordModel z wstrzykiwaniem zale�no�ci
        public ChangePasswordModel(AuditLogService auditLogService,UserService userService, PasswordService passwordService, PasswordPolicyService policyService, PasswordValidationService validationService)
        {
            _userService = userService;
            _passwordService = passwordService;
            _policyService = policyService;
            _validationService = validationService;
            _auditLogService = auditLogService;
        }
        // Model powi�zany z formularzem zmiany has�a
        [BindProperty]
        public InputModel Input { get; set; } = new();

        public string? ErrorMessage { get; set; }

        public class InputModel
        {
            [Required(ErrorMessage = "Stare has�o jest wymagane")]
            [DataType(DataType.Password)]
            [Display(Name = "Stare has�o")]
            public string OldPassword { get; set; } = string.Empty;

            [Required(ErrorMessage = "Nowe has�o jest wymagane")]
            [StringLength(100, ErrorMessage = "Has�o musi mie� co najmniej 6 znak�w.", MinimumLength = 6)]
            [DataType(DataType.Password)]
            [Display(Name = "Nowe has�o")]
            public string NewPassword { get; set; } = string.Empty;

            [DataType(DataType.Password)]
            [Display(Name = "Potwierd� nowe has�o")]
            [Compare("NewPassword", ErrorMessage = "Has�a nie s� takie same.")]
            public string ConfirmPassword { get; set; } = string.Empty;
        }
        // Obs�uga ��dania POST do zmiany has�a
        public async Task<IActionResult> OnPostAsync()
        {
            //Sprawdzenie poprawno�ci modelu
            if (!ModelState.IsValid)
            {
                return Page();
            }
            // Pobranie polityki hase� i walidacja nowego has�a
            var policy = await _policyService.GetSettingsAsync();
            var validationError = _validationService.Validate(Input.NewPassword, policy);
            if (validationError != null)
            {
                ModelState.AddModelError(string.Empty, validationError);
                return Page();
            }
            // Pobranie u�ytkownika i zmiana has�a
            var username = User.Identity?.Name;
            if (username == null)
            {
                return RedirectToPage("/Login");
            }
            var user = await _userService.GetByUsernameAsync(username);
            if (user == null || !_passwordService.VerifyPassword(Input.OldPassword, user.PasswordHash))
            {
                ModelState.AddModelError("Input.OldPassword", "Stare has�o jest nieprawid�owe.");
                return Page();
            }
            // Sprawdzenie historii hase�

            foreach (var oldHash in user.PasswordHistory)
            {
                if (_passwordService.VerifyPassword(Input.NewPassword, oldHash))
                {
                    ModelState.AddModelError("Input.NewPassword", "Nowe has�o nie mo�e by� takie samo jak jedno z 5 poprzednich.");
                    return Page();
                }
            }
            // Aktualizacja has�a i historii hase�
            user.PasswordHistory.Add(user.PasswordHash);

            // Utrzymanie tylko ostatnich 5 hase� w historii
            while (user.PasswordHistory.Count > 5)
            {
                user.PasswordHistory.RemoveAt(0);
            }
            // Ustawienie nowego has�a
            user.PasswordHash = _passwordService.HashPassword(Input.NewPassword);
            user.MustChangePassword = false;
            user.PasswordLastSet = DateTime.UtcNow;
            // Zapisanie zmian w bazie danych
            await _userService.UpdateAsync(user);
            await _auditLogService.LogAsync(username, "Zmiana has�a", "U�ytkownik zosta� zmuszony do zmiany has�a.");

            return RedirectToPage("/Index");
        }
    }
}