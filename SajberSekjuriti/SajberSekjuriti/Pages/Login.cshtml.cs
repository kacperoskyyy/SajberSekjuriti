using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MongoDB.Driver.Linq;
using SajberSekjuriti.Model;
using SajberSekjuriti.Services;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace SajberSekjuriti.Pages;

public class LoginModel : PageModel
{
    private readonly UserService _userService;
    private readonly PasswordService _passwordService;
    private readonly PasswordPolicyService _policyService;
    private readonly AuditLogService _auditLogService;

    //Konstruktor z wstrzykiwaniem zale¿noœci
    public LoginModel(AuditLogService auditLogService, UserService userService, PasswordService passwordService, PasswordPolicyService policyService)
    {
        _userService = userService;
        _passwordService = passwordService;
        _policyService = policyService;
        _auditLogService = auditLogService;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public string? ErrorMessage { get; set; }

    // Model do przechowywania danych wejœciowych
    public class InputModel
    {
        [Required(ErrorMessage = "Login jest wymagany")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "Has³o jest wymagane")]
        public string Password { get; set; } = string.Empty;
    }

    // Obs³uga ¿¹dania POST
    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        // Pobieramy politykê bezpieczeñstwa OD RAZU
        var policy = await _policyService.GetSettingsAsync();
        var maxAttempts = policy.MaxLoginAttempts ?? 0;
        var lockoutMinutes = policy.LockoutDurationMinutes ?? 15; // Domyœlnie 15 min

        var user = await _userService.GetByUsernameAsync(Input.Username);

        // --- Krok 1: SprawdŸ, czy user w ogóle istnieje lub jest zablokowany przez admina
        if (user == null || user.IsBlocked)
        {
            await _auditLogService.LogAsync(Input.Username, "B³êdne logowanie", "Nieudana próba logowania (u¿ytkownik nie istnieje lub zablokowany).");
            ErrorMessage = "Login lub has³o niepoprawne.";
            return Page();
        }

        // --- Krok 2: SprawdŸ, czy konto jest zablokowane czasowo
        if (user.LockoutEndDate.HasValue && user.LockoutEndDate > DateTime.UtcNow)
        {
            var remainingTime = user.LockoutEndDate.Value - DateTime.UtcNow;
            ErrorMessage = $"BLOKADA: Konto zablokowane. Spróbuj ponownie za {remainingTime.Minutes} min {remainingTime.Seconds} sek.";
            await _auditLogService.LogAsync(Input.Username, "B³êdne logowanie", "Nieudana próba logowania (konto czasowo zablokowane).");
            return Page();
        }

        // --- Krok 3: SprawdŸ has³o
        if (!_passwordService.VerifyPassword(Input.Password, user.PasswordHash))
        {
            // Has³o jest B£ÊDNE. Czas w³¹czyæ licznik.
            await _auditLogService.LogAsync(Input.Username, "B³êdne logowanie", "Nieudana próba logowania (niepoprawne has³o).");

            if (maxAttempts > 0) // Sprawdzamy, czy polityka blokad jest w³¹czona
            {
                user.FailedLoginAttempts++;
                if (user.FailedLoginAttempts >= maxAttempts)
                {
                    // U¿ytkownik przekroczy³ limit! Blokujemy konto.
                    user.LockoutEndDate = DateTime.UtcNow.AddMinutes(lockoutMinutes);
                    user.FailedLoginAttempts = 0; // Resetujemy licznik na nastêpn¹ blokadê
                    ErrorMessage = $"BLOKADA: Wprowadzono niepoprawne dane {maxAttempts} razy. Konto zablokowane na {lockoutMinutes} minut.";
                }
                else
                {
                    ErrorMessage = $"Login lub has³o niepoprawne. Pozosta³o prób: {maxAttempts - user.FailedLoginAttempts}";
                }
                await _userService.UpdateAsync(user);
            }
            else
            {
                ErrorMessage = "Login lub has³o niepoprawne.";
            }

            return Page();
        }

        // --- Has³o jest POPRAWNE ---

        // Resetujemy licznik b³êdów i blokadê
        user.FailedLoginAttempts = 0;
        user.LockoutEndDate = null;

        // Sprawdzamy wa¿noœæ has³a
        int? expirationDays = user.PasswordExpirationDays ?? policy.PasswordExpirationDays;
        if (expirationDays.HasValue && expirationDays > 0 && user.PasswordLastSet.HasValue)
        {
            if (DateTime.UtcNow > user.PasswordLastSet.Value.AddDays(expirationDays.Value))
            {
                user.MustChangePassword = true;
            }
        }

        await _userService.UpdateAsync(user);



        if (user.IsOneTimePasswordEnabled)
        {

            await _auditLogService.LogAsync(user.Username, "Logowanie OTP", "Etap 1/2: Has³o poprawne. Przekierowanie do has³a jednorazowego.");


            TempData["OTPUsername"] = user.Username;

            // Przekierowujemy na stronê wprowadzania OTP.
            return RedirectToPage("/LoginOTP");
        }

        await _auditLogService.LogAsync(user.Username, "Logowanie", "U¿ytkownik zalogowa³ siê pomyœlnie (bez OTP).");

        // --- Standardowa logika tworzenia ciasteczka ---
        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, user.Username),
            new(ClaimTypes.Role, user.Role.ToString())
        };

        var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(claimsIdentity));

        if (user.MustChangePassword)
        {
            return RedirectToPage("/ChangePassword");
        }

        return RedirectToPage("/Index");
    }
}