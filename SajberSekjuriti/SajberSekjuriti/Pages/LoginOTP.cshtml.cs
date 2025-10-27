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
using System.Globalization; // Potrzebne do parsowania double

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
            [Display(Name = "Obliczony kod OTP")] // Zmieniona etykieta
            public string OTPCode { get; set; } = string.Empty;
        }

        // Metoda OnGetAsync pozostaje BEZ ZMIAN
        // (Nadal generuje 'a' i 'x' i zapisuje je w TempData)
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

            TempData["OTP_a"] = a;
            TempData["OTP_x"] = x;
            Input.Username = usernameFromTempData;

            TempData.Keep("OTPUsername");
            TempData.Keep("OTP_a");
            TempData.Keep("OTP_x");

            return Page();
        }

        // Metoda OnPostAsync jest ZAKTUALIZOWANA
        public async Task<IActionResult> OnPostAsync()
        {
            // Odzyskujemy 'a' i 'x' z TempData
            int a = Convert.ToInt32(TempData["OTP_a"]);
            int x = Convert.ToInt32(TempData["OTP_x"]);

            // Ustawiamy w�a�ciwo�ci widoku na wypadek b��du
            Username = Input.Username;
            LoginLengthA = a;
            RandomNumberX = x;

            if (!ModelState.IsValid)
            {
                return Page();
            }

            var user = await _userService.GetByUsernameAsync(Input.Username);

            if (user == null || !user.IsOneTimePasswordEnabled)
            {
                await _auditLogService.LogAsync(Input.Username, "B��d logowania OTP", "Pr�ba weryfikacji OTP (u�ytkownik nie istnieje lub OTP wy��czone).");
                return RedirectToPage("/Login");
            }

            // --- NOWA LOGIKA WERYFIKACJI: lg(a*x) z zaokr�gleniem ---

            bool isOtpValid = false;

            // 1. Obliczamy oczekiwany wynik (lg to logarytm dziesi�tny)
            double result = Math.Log10(a * x);

            // 2. Zaokr�glamy do 2 miejsc po przecinku
            double expectedOtpValue = Math.Round(result, 2);

            // 3. Parsujemy wej�cie u�ytkownika jako double.
            // U�ywamy InvariantCulture, aby kropka (.) by�a separatorem, a nie przecinek (,)
            if (double.TryParse(Input.OTPCode, NumberStyles.Any, CultureInfo.InvariantCulture, out double userOtpCode))
            {
                // 4. Por�wnujemy warto�ci double
                if (userOtpCode == expectedOtpValue)
                {
                    isOtpValid = true;
                }
            }
            // --- Koniec nowej logiki ---

            if (!isOtpValid)
            {
                // W celach testowych/debugowych mo�na pokaza� oczekiwany wynik
                ErrorMessage = $"Niepoprawny kod. (Oczekiwano: {expectedOtpValue.ToString("F2", CultureInfo.InvariantCulture)})";
                await _auditLogService.LogAsync(Input.Username, "Logowanie OTP", "Etap 2/2: Nieudana pr�ba logowania (niepoprawne has�o jednorazowe).");
                return Page();
            }

            // --- SUKCES! Kod OTP jest poprawny ---

            await _auditLogService.LogAsync(user.Username, "Logowanie OTP", "Etap 2/2: Has�o jednorazowe poprawne. Zalogowano.");

            // OSTATECZNIE LOGUJEMY U�YTKOWNIKA
            var claims = new List<Claim>
            {
                new(ClaimTypes.Name, user.Username),
                new(ClaimTypes.Role, user.Role.ToString())
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity));

            // Czy�cimy TempData
            TempData.Remove("OTPUsername");
            TempData.Remove("OTP_a");
            TempData.Remove("OTP_x");

            if (user.MustChangePassword)
            {
                return RedirectToPage("/ChangePassword");
            }

            return RedirectToPage("/Index");
        }
    }
}