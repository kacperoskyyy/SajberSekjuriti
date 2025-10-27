using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SajberSekjuriti.Model;
using SajberSekjuriti.Services;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Threading.Tasks;
using System;
using System.Globalization;

namespace SajberSekjuriti.Pages
{
    public class LoginOTPModel : PageModel
    {
        private readonly UserService _userService;
        private readonly AuditLogService _auditLogService;
        private static readonly Random _random = new Random();

        public LoginOTPModel(UserService userService, AuditLogService auditLogService)
        {
            _userService = userService;
            _auditLogService = auditLogService;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public string Username { get; set; } = string.Empty;
        public int LoginLengthA { get; set; }
        public int RandomNumberX { get; set; }

        public string? ErrorMessage { get; set; }

        public class InputModel
        {
            public string Username { get; set; } = string.Empty;

            [Required(ErrorMessage = "Kod OTP jest wymagany")]
            [Display(Name = "Obliczony kod OTP")]
            public string OTPCode { get; set; } = string.Empty;

            public int LoginLengthA { get; set; }
            public int RandomNumberX { get; set; }
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var usernameFromTempData = TempData["OTPUsername"]?.ToString();

            if (string.IsNullOrEmpty(usernameFromTempData))
            {
                return RedirectToPage("/Login");
            }

            int a = usernameFromTempData.Length;
            int x = _random.Next(100, 1000);

            Username = usernameFromTempData;
            LoginLengthA = a;
            RandomNumberX = x;

            Input.Username = usernameFromTempData;
            Input.LoginLengthA = a;
            Input.RandomNumberX = x;

            TempData.Keep("OTPUsername");

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            int a = Input.LoginLengthA;
            int x = Input.RandomNumberX;

            Username = Input.Username;
            LoginLengthA = a;
            RandomNumberX = x;

            if (!ModelState.IsValid)
            {
                return Page();
            }

            if (a == 0 || x == 0)
            {
                ErrorMessage = "Wyst¹pi³ b³¹d sesji (a=0 lub x=0). Spróbuj zalogowaæ siê ponownie.";
                return RedirectToPage("/Login");
            }

            var user = await _userService.GetByUsernameAsync(Input.Username);

            if (user == null || !user.IsOneTimePasswordEnabled)
            {
                return RedirectToPage("/Login");
            }

            bool isOtpValid = false;
            double result = Math.Log10(a * x);
            double expectedOtpValue = Math.Round(result, 2);

            if (double.TryParse(Input.OTPCode, NumberStyles.Any, CultureInfo.InvariantCulture, out double userOtpCode))
            {
                if (userOtpCode == expectedOtpValue)
                {
                    isOtpValid = true;
                }
            }

            if (!isOtpValid)
            {
                await _auditLogService.LogAsync(Input.Username, "Logowanie OTP", "Etap 2/2: Nieudana próba logowania (niepoprawne has³o jednorazowe).");

                TempData["OTPError"] = "B³êdne has³o jednorazowe. Spróbuj zalogowaæ siê ponownie.";
                return RedirectToPage("/Login");
            }

            await _auditLogService.LogAsync(user.Username, "Logowanie OTP", "Etap 2/2: Has³o jednorazowe poprawne. Zalogowano.");

            var claims = new List<Claim>
            {
                new(ClaimTypes.Name, user.Username),
                new(ClaimTypes.Role, user.Role.ToString())
            };
            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity));

            TempData.Remove("OTPUsername");

            if (user.MustChangePassword)
            {
                return RedirectToPage("/ChangePassword");
            }

            return RedirectToPage("/Index");
        }
    }
}