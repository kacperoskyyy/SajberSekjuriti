using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MongoDB.Driver.Linq;
using SajberSekjuriti.Model;
using SajberSekjuriti.Services;
using SajberSekjuriti.Services;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace SajberSekjuriti.Pages;

public class LoginModel : PageModel
{
    private readonly UserService _userService;
    private readonly PasswordService _passwordService;
    private readonly PasswordPolicyService _policyService;

    //Konstruktor z wstrzykiwaniem zale�no�ci
    public LoginModel(UserService userService, PasswordService passwordService, PasswordPolicyService policyService)
    {
        _userService = userService;
        _passwordService = passwordService;
        _policyService = policyService;
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
        // Walidacja modelu
        if (!ModelState.IsValid)
        {
            return Page();
        }
        // Pobieranie u�ytkownika z bazy danych
        var user = await _userService.GetByUsernameAsync(Input.Username);
        if (user == null || user.IsBlocked || !_passwordService.VerifyPassword(Input.Password, user.PasswordHash))
        {
            ErrorMessage = "Login lub has�o niepoprawne.";
            return Page();
        }
        // Sprawdzanie polityki hase�
        var policy = await _policyService.GetSettingsAsync();

        // Najpierw bierzemy ustawienie indywidualne. Je�li go nie ma, bierzemy globalne.
        int? expirationDays = user.PasswordExpirationDays ?? policy.PasswordExpirationDays;

        if (expirationDays.HasValue && expirationDays > 0 && user.PasswordLastSet.HasValue)
        {
            // U�ywamy .Value, bo jeste�my pewni, �e warto�� istnieje
            if (DateTime.UtcNow > user.PasswordLastSet.Value.AddDays(expirationDays.Value))
            {
                // Has�o wygas�o! Zmuszamy do zmiany.
                user.MustChangePassword = true;
                await _userService.UpdateAsync(user);
            }
        }
        // Tworzenie to�samo�ci u�ytkownika
        var claims = new List<Claim>
    {
        new(ClaimTypes.Name, user.Username),
        new(ClaimTypes.Role, user.Role.ToString())
    };

        var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        // Logowanie u�ytkownika
        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(claimsIdentity));

        // Sprawdzamy, czy u�ytkownik musi zmieni� has�o
        if (user.MustChangePassword)
        {
            return RedirectToPage("/ChangePassword");
        }

        return RedirectToPage("/Index"); // Przekierowanie na stron� g��wn�
    }
}