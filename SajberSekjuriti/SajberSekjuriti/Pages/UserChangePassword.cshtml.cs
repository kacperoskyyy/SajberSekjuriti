using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SajberSekjuriti.Model;
using SajberSekjuriti.Services;
using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Configuration; // <-- Dodane
using System.Threading.Tasks; // <-- Dodane

namespace SajberSekjuriti.Pages
{
    [Authorize]
    public class UserChangePasswordModel : PageModel
    {
        private readonly UserService _userService;
        private readonly PasswordService _passwordService;
        private readonly PasswordPolicyService _policyService;
        private readonly PasswordValidationService _validationService;
        private readonly AuditLogService _auditLogService;

        // --- DODANE SERWISY ---
        private readonly ReCaptchaService _reCaptchaService;
        private readonly IConfiguration _configuration;

        //Konstruktor klasy UserChangePasswordModel, kt�ry inicjalizuje serwisy potrzebne do zmiany has�a u�ytkownika.
        public UserChangePasswordModel(
            AuditLogService auditLogService,
            UserService userService,
            PasswordService passwordService,
            PasswordPolicyService policyService,
            PasswordValidationService validationService,
            ReCaptchaService reCaptchaService, // <-- Dodane
            IConfiguration configuration) // <-- Dodane
        {
            _userService = userService;
            _passwordService = passwordService;
            _policyService = policyService;
            _validationService = validationService;
            _auditLogService = auditLogService;
            _reCaptchaService = reCaptchaService; // <-- Dodane
            _configuration = configuration; // <-- Dodane
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        // --- DODANA W�A�CIWO�� ---
        // Do przekazania klucza publicznego do widoku .cshtml
        public string ReCaptchaSiteKey => _configuration["ReCaptchaSettings:SiteKey"];

        // Dodajemy, aby wy�wietla� b��dy reCaptcha
        public string? ErrorMessage { get; set; }

        //Definicja klasy InputModel, kt�ra zawiera w�a�ciwo�ci potrzebne do zmiany has�a u�ytkownika.
        public class InputModel
        {
            [Required(ErrorMessage = "Stare has�o jest wymagane")]
            [DataType(DataType.Password)]
            [Display(Name = "Stare has�o")]
            public string OldPassword { get; set; } = string.Empty;

            [Required(ErrorMessage = "Nowe has�o jest wymagane")]
            [DataType(DataType.Password)]
            [Display(Name = "Nowe has�o")]
            public string NewPassword { get; set; } = string.Empty;

            [DataType(DataType.Password)]
            [Display(Name = "Potwierd� nowe has�o")]
            [Compare("NewPassword", ErrorMessage = "Has�a nie s� takie same.")]
            public string ConfirmPassword { get; set; } = string.Empty;
        }

        //Metoda obs�uguj�ca ��dania POST do strony zmiany has�a.
        public async Task<IActionResult> OnPostAsync()
        {
            // --- KROK 1: Weryfikacja Google reCaptcha ---
            var reCaptchaToken = Request.Form["g-recaptcha-response"];
            var isReCaptchaValid = await _reCaptchaService.ValidateAsync(reCaptchaToken);

            if (!isReCaptchaValid)
            {
                ErrorMessage = "Weryfikacja CAPTCHA nie powiod�a si�. Spr�buj ponownie.";
                return Page();
            }

            // --- KROK 2: Twoja dotychczasowa logika ---

            //Sprawdzenie, czy dane wej�ciowe s� poprawne.
            if (!ModelState.IsValid)
            {
                return Page();
            }
            //Pobranie nazwy u�ytkownika z kontekstu i sprawdzenie, czy u�ytkownik jest zalogowany.
            var username = User.Identity?.Name;
            if (string.IsNullOrEmpty(username))
            {
                return RedirectToPage("/Login");
            }
            //Pobranie polityki hase� i walidacja nowego has�a u�ytkownika.
            var policy = await _policyService.GetSettingsAsync();
            var validationError = _validationService.Validate(Input.NewPassword, policy);
            if (validationError != null)
            {
                ModelState.AddModelError(string.Empty, validationError);
                return Page();
            }
            //Pobranie u�ytkownika z bazy danych i sprawdzenie, czy stare has�o jest poprawne.
            var user = await _userService.GetByUsernameAsync(username);
            if (user == null || !_passwordService.VerifyPassword(Input.OldPassword, user.PasswordHash))
            {
                ModelState.AddModelError("Input.OldPassword", "Stare has�o jest nieprawid�owe.");
                return Page();
            }

            //SPRAWDZANIE HISTORII HAS�A
            foreach (var oldHash in user.PasswordHistory)
            {
                if (_passwordService.VerifyPassword(Input.NewPassword, oldHash))
                {
                    ModelState.AddModelError("Input.NewPassword", "Nowe has�o nie mo�e by� takie samo jak jedno z 5 poprzednich.");
                    return Page();
                }
            }
            user.PasswordHistory.Add(user.PasswordHash);

            while (user.PasswordHistory.Count > 5)
            {
                user.PasswordHistory.RemoveAt(0);
            }
            //Aktualizacja has�a u�ytkownika i zapisanie zmian w bazie danych.
            user.PasswordHash = _passwordService.HashPassword(Input.NewPassword);
            user.PasswordLastSet = DateTime.UtcNow;
            user.MustChangePassword = false;

            await _userService.UpdateAsync(user);
            await _auditLogService.LogAsync(username, "Zmiana has�a", "U�ytkownik samodzielnie zmieni� has�o.");

            TempData["SuccessMessage"] = "Has�o zosta�o pomy�lnie zmienione.";
            return RedirectToPage("/Index");
        }
    }
}