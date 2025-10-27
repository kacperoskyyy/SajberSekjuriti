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

    //Konstruktor z wstrzykiwaniem zale�no�ci
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

    // Model do przechowywania danych wej�ciowych
    public class InputModel
    {
        [Required(ErrorMessage = "Login jest wymagany")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "Has�o jest wymagane")]
        public string Password { get; set; } = string.Empty;
    }

    // Obs�uga ��dania POST
    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        // Pobieramy polityk� bezpiecze�stwa OD RAZU
        var policy = await _policyService.GetSettingsAsync();
        var maxAttempts = policy.MaxLoginAttempts ?? 0;
        var lockoutMinutes = policy.LockoutDurationMinutes ?? 15; // Domy�lnie 15 min

        var user = await _userService.GetByUsernameAsync(Input.Username);

        // --- Krok 1: Sprawd�, czy user w og�le istnieje lub jest zablokowany przez admina
        if (user == null || user.IsBlocked)
        {
            await _auditLogService.LogAsync(Input.Username, "B��dne logowanie", "Nieudana pr�ba logowania (u�ytkownik nie istnieje lub zablokowany).");
            ErrorMessage = "Login lub has�o niepoprawne.";
            return Page();
        }

        // --- Krok 2: Sprawd�, czy konto jest zablokowane czasowo
        if (user.LockoutEndDate.HasValue && user.LockoutEndDate > DateTime.UtcNow)
        {
            var remainingTime = user.LockoutEndDate.Value - DateTime.UtcNow;
            ErrorMessage = $"BLOKADA: Konto zablokowane. Spr�buj ponownie za {remainingTime.Minutes} min {remainingTime.Seconds} sek.";
            await _auditLogService.LogAsync(Input.Username, "B��dne logowanie", "Nieudana pr�ba logowania (konto czasowo zablokowane).");
            return Page();
        }

        // --- Krok 3: Sprawd� has�o
        if (!_passwordService.VerifyPassword(Input.Password, user.PasswordHash))
        {
            // Has�o jest B��DNE. Czas w��czy� licznik.
            await _auditLogService.LogAsync(Input.Username, "B��dne logowanie", "Nieudana pr�ba logowania (niepoprawne has�o).");

            if (maxAttempts > 0) // Sprawdzamy, czy polityka blokad jest w��czona
            {
                user.FailedLoginAttempts++;
                if (user.FailedLoginAttempts >= maxAttempts)
                {
                    // U�ytkownik przekroczy� limit! Blokujemy konto.
                    user.LockoutEndDate = DateTime.UtcNow.AddMinutes(lockoutMinutes);
                    user.FailedLoginAttempts = 0; // Resetujemy licznik na nast�pn� blokad�
                    ErrorMessage = $"BLOKADA: Wprowadzono niepoprawne dane {maxAttempts} razy. Konto zablokowane na {lockoutMinutes} minut.";
                }
                else
                {
                    ErrorMessage = $"Login lub has�o niepoprawne. Pozosta�o pr�b: {maxAttempts - user.FailedLoginAttempts}";
                }
                await _userService.UpdateAsync(user);
            }
            else
            {
                ErrorMessage = "Login lub has�o niepoprawne.";
            }

            return Page();
        }

        // --- Has�o jest POPRAWNE ---

        // Resetujemy licznik b��d�w i blokad�
        user.FailedLoginAttempts = 0;
        user.LockoutEndDate = null;

        // Sprawdzamy wa�no�� has�a
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

            await _auditLogService.LogAsync(user.Username, "Logowanie OTP", "Etap 1/2: Has�o poprawne. Przekierowanie do has�a jednorazowego.");


            TempData["OTPUsername"] = user.Username;

            // Przekierowujemy na stron� wprowadzania OTP.
            return RedirectToPage("/LoginOTP");
        }

        await _auditLogService.LogAsync(user.Username, "Logowanie", "U�ytkownik zalogowa� si� pomy�lnie (bez OTP).");

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