using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using SajberSekjuriti.Model;
using SajberSekjuriti.Services;
using System.ComponentModel.DataAnnotations;
using System.Data;

namespace SajberSekjuriti.Pages
{
    [Authorize(Roles = "Admin")]
    public class EditUserModel : PageModel
    {
        private readonly UserService _userService;
        private readonly PasswordService _passwordService;
        private readonly PasswordPolicyService _policyService;
        private readonly PasswordValidationService _validationService;
        private readonly AuditLogService _auditLogService;

        // Konstruktor klasy EditUserModel z wstrzykiwaniem zale¿noœci
        public EditUserModel(AuditLogService auditLogService ,UserService userService, PasswordService passwordService, PasswordPolicyService policyService, PasswordValidationService validationService)
        {
            _userService = userService;
            _passwordService = passwordService;
            _policyService = policyService;
            _validationService = validationService;
            _auditLogService = auditLogService;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        // Klasa reprezentuj¹ca dane wejœciowe formularza
        public class InputModel
        {
            [Required]
            public string Id { get; set; } = string.Empty;

            [Required(ErrorMessage = "Login jest wymagany")]
            public string Username { get; set; } = string.Empty;

            [Required(ErrorMessage = "Pe³na nazwa jest wymagana")]
            [Display(Name = "Pe³na nazwa")]
            public string FullName { get; set; } = string.Empty;

            [Required]
            [Display(Name = "Rola")]
            public UserRole Role { get; set; }

            [Display(Name = "Nowe has³o (zostaw puste, jeœli bez zmian)")]
            [DataType(DataType.Password)]
            public string? NewPassword { get; set; }
            [Display(Name = "Indywidualna wa¿noœæ has³a (w dniach, puste = globalna)")]
            public int? PasswordExpirationDays { get; set; }
        }

        // Obs³uga ¿¹dania GET do za³adowania danych u¿ytkownika
        public async Task<IActionResult> OnGetAsync(string id)
        {
            var user = await _userService.GetByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            Input = new InputModel
            {
                Id = user.Id,
                Username = user.Username,
                FullName = user.FullName,
                Role = user.Role,
                PasswordExpirationDays = user.PasswordExpirationDays
            };
            return Page();
        }

        // Obs³uga ¿¹dania POST do aktualizacji danych u¿ytkownika
        public async Task<IActionResult> OnPostAsync()
        {
            // Walidacja indywidualnej wa¿noœci has³a
            int? expirationDays = null;
            if (int.TryParse(Input.PasswordExpirationDays.ToString(), out int parsedDays))
            {
                expirationDays = parsedDays;
                //Console.WriteLine($"parsed: {parsedDays}, expirationDays: {expirationDays}");
            }
            else if (!string.IsNullOrEmpty(Input.PasswordExpirationDays.ToString()))
            {
                ModelState.AddModelError("Input.PasswordExpirationDays", "Wartoœæ musi byæ poprawn¹ liczb¹.");
            }

            // Sprawdzenie poprawnoœci modelu
            if (!ModelState.IsValid)
            {
                return Page();
            }
            // Pobranie u¿ytkownika z bazy danych
            var user = await _userService.GetByIdAsync(Input.Id);
            if (user == null)
            {
                return NotFound();
            }

            // Aktualizacja danych u¿ytkownika
            user.FullName = Input.FullName;
            user.Role = Input.Role;
            user.PasswordExpirationDays = expirationDays;

            // Jeœli podano nowe has³o, przeprowadŸ walidacjê i aktualizacjê
            if (!string.IsNullOrEmpty(Input.NewPassword))
            {
                // Pobranie polityki hase³ i walidacja nowego has³a
                var policy = await _policyService.GetSettingsAsync();
                var validationError = _validationService.Validate(Input.NewPassword, policy);
                if (validationError != null)
                {
                    ModelState.AddModelError(string.Empty, validationError);
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
                while (user.PasswordHistory.Count > 5)
                {
                    user.PasswordHistory.RemoveAt(0);
                }
                user.PasswordHash = _passwordService.HashPassword(Input.NewPassword);
            }

            // Zapisanie zmian w bazie danych
            await _userService.UpdateAsync(user);
            await _auditLogService.LogAsync(User.Identity.Name, "Zarz¹dzanie", $"Admin zaktualizowa³ profil u¿ytkownika {user.Username}.");

            return RedirectToPage("/AdminPanel");
        }
    }
}