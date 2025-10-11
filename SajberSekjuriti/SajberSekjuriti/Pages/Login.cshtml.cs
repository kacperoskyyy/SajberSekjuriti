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

    public LoginModel(UserService userService, PasswordService passwordService, PasswordPolicyService policyService)
    {
        _userService = userService;
        _passwordService = passwordService;
        _policyService = policyService;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public string? ErrorMessage { get; set; }

    public class InputModel
    {
        [Required(ErrorMessage = "Login jest wymagany")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "Has³o jest wymagane")]
        public string Password { get; set; } = string.Empty;
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var user = await _userService.GetByUsernameAsync(Input.Username);
        if (user == null || user.IsBlocked || !_passwordService.VerifyPassword(Input.Password, user.PasswordHash))
        {
            ErrorMessage = "Login lub has³o niepoprawne.";
            return Page();
        }

        var policy = await _policyService.GetSettingsAsync();
        if (policy.PasswordExpirationDays > 0 && user.PasswordLastSet.HasValue)
        {
            if (DateTime.UtcNow > user.PasswordLastSet.Value.AddDays(policy.PasswordExpirationDays.Value))
            {
                user.MustChangePassword = true;
                await _userService.UpdateAsync(user);
            }
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, user.Username),
            new(ClaimTypes.Role, user.Role.ToString())
        };

        var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(claimsIdentity));

        // Sprawdzamy, czy u¿ytkownik musi zmieniæ has³o
        if (user.MustChangePassword)
        {
            return RedirectToPage("/ChangePassword");
        }

        return RedirectToPage("/Index"); // Przekierowanie na stronê g³ówn¹
    }
}