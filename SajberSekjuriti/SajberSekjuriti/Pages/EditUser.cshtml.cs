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

        // Konstruktor klasy EditUserModel z wstrzykiwaniem zale�no�ci
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

        // Klasa reprezentuj�ca dane wej�ciowe formularza
        public class InputModel
        {
            [Required]
            public string Id { get; set; } = string.Empty;

            [Required(ErrorMessage = "Login jest wymagany")]
            public string Username { get; set; } = string.Empty;

            [Required(ErrorMessage = "Pe�na nazwa jest wymagana")]
            [Display(Name = "Pe�na nazwa")]
            public string FullName { get; set; } = string.Empty;

            [Required]
            [Display(Name = "Rola")]
            public UserRole Role { get; set; }

            [Display(Name = "Nowe has�o (zostaw puste, je�li bez zmian)")]
            [DataType(DataType.Password)]
            public string? NewPassword { get; set; }
            [Display(Name = "Indywidualna wa�no�� has�a (w dniach, puste = globalna)")]
            public int? PasswordExpirationDays { get; set; }
        }

        // Obs�uga ��dania GET do za�adowania danych u�ytkownika
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

        // Obs�uga ��dania POST do aktualizacji danych u�ytkownika
        public async Task<IActionResult> OnPostAsync()
        {
            // Walidacja indywidualnej wa�no�ci has�a
            int? expirationDays = null;
            if (int.TryParse(Input.PasswordExpirationDays.ToString(), out int parsedDays))
            {
                expirationDays = parsedDays;
                //Console.WriteLine($"parsed: {parsedDays}, expirationDays: {expirationDays}");
            }
            else if (!string.IsNullOrEmpty(Input.PasswordExpirationDays.ToString()))
            {
                ModelState.AddModelError("Input.PasswordExpirationDays", "Warto�� musi by� poprawn� liczb�.");
            }

            // Sprawdzenie poprawno�ci modelu
            if (!ModelState.IsValid)
            {
                return Page();
            }
            // Pobranie u�ytkownika z bazy danych
            var user = await _userService.GetByIdAsync(Input.Id);
            if (user == null)
            {
                return NotFound();
            }

            // Aktualizacja danych u�ytkownika
            user.FullName = Input.FullName;
            user.Role = Input.Role;
            user.PasswordExpirationDays = expirationDays;

            // Je�li podano nowe has�o, przeprowad� walidacj� i aktualizacj�
            if (!string.IsNullOrEmpty(Input.NewPassword))
            {
                // Pobranie polityki hase� i walidacja nowego has�a
                var policy = await _policyService.GetSettingsAsync();
                var validationError = _validationService.Validate(Input.NewPassword, policy);
                if (validationError != null)
                {
                    ModelState.AddModelError(string.Empty, validationError);
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
                while (user.PasswordHistory.Count > 5)
                {
                    user.PasswordHistory.RemoveAt(0);
                }
                user.PasswordHash = _passwordService.HashPassword(Input.NewPassword);
            }

            // Zapisanie zmian w bazie danych
            await _userService.UpdateAsync(user);
            await _auditLogService.LogAsync(User.Identity.Name, "Zarz�dzanie", $"Admin zaktualizowa� profil u�ytkownika {user.Username}.");

            return RedirectToPage("/AdminPanel");
        }
    }
}