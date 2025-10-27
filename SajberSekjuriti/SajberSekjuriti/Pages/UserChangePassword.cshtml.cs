using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SajberSekjuriti.Services;
using System.ComponentModel.DataAnnotations;

namespace SajberSekjuriti.Pages;

[Authorize]
public class UserChangePasswordModel : PageModel
{
    private readonly UserService _userService;
    private readonly PasswordService _passwordService;
    private readonly PasswordPolicyService _policyService;
    private readonly PasswordValidationService _validationService;
    private readonly AuditLogService _auditLogService;
    private readonly ReCaptchaService _reCaptchaService;
    private readonly IConfiguration _configuration;

    public UserChangePasswordModel(
        AuditLogService auditLogService,
        UserService userService,
        PasswordService passwordService,
        PasswordPolicyService policyService,
        PasswordValidationService validationService,
        ReCaptchaService reCaptchaService,
        IConfiguration configuration)
    {
        _userService = userService;
        _passwordService = passwordService;
        _policyService = policyService;
        _validationService = validationService;
        _auditLogService = auditLogService;
        _reCaptchaService = reCaptchaService;
        _configuration = configuration;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public string ReCaptchaSiteKey => _configuration["ReCaptchaSettings:SiteKey"];
    public string? ErrorMessage { get; set; }

    public class InputModel
    {
        [Required(ErrorMessage = "Stare has≥o jest wymagane")]
        [DataType(DataType.Password)]
        [Display(Name = "Stare has≥o")]
        public string OldPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Nowe has≥o jest wymagane")]
        [DataType(DataType.Password)]
        [Display(Name = "Nowe has≥o")]
        public string NewPassword { get; set; } = string.Empty;

        [DataType(DataType.Password)]
        [Display(Name = "Potwierdü nowe has≥o")]
        [Compare("NewPassword", ErrorMessage = "Has≥a nie sπ takie same.")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var reCaptchaToken = Request.Form["g-recaptcha-response"];
        var isReCaptchaValid = await _reCaptchaService.ValidateAsync(reCaptchaToken);

        if (!isReCaptchaValid)
        {
            ErrorMessage = "Weryfikacja CAPTCHA nie powiod≥a siÍ. SprÛbuj ponownie.";
            return Page();
        }

        if (!ModelState.IsValid)
        {
            return Page();
        }
        var username = User.Identity?.Name;
        if (string.IsNullOrEmpty(username))
        {
            return RedirectToPage("/Login");
        }
        var policy = await _policyService.GetSettingsAsync();
        var validationError = _validationService.Validate(Input.NewPassword, policy);
        if (validationError != null)
        {
            ModelState.AddModelError(string.Empty, validationError);
            return Page();
        }
        var user = await _userService.GetByUsernameAsync(username);
        if (user == null || !_passwordService.VerifyPassword(Input.OldPassword, user.PasswordHash))
        {
            ModelState.AddModelError("Input.OldPassword", "Stare has≥o jest nieprawid≥owe.");
            return Page();
        }

        foreach (var oldHash in user.PasswordHistory)
        {
            if (_passwordService.VerifyPassword(Input.NewPassword, oldHash))
            {
                ModelState.AddModelError("Input.NewPassword", "Nowe has≥o nie moøe byÊ takie samo jak jedno z 5 poprzednich.");
                return Page();
            }
        }
        user.PasswordHistory.Add(user.PasswordHash);

        while (user.PasswordHistory.Count > 5)
        {
            user.PasswordHistory.RemoveAt(0);
        }
        user.PasswordHash = _passwordService.HashPassword(Input.NewPassword);
        user.PasswordLastSet = DateTime.UtcNow;
        user.MustChangePassword = false;

        await _userService.UpdateAsync(user);
        await _auditLogService.LogAsync(username, "Zmiana has≥a", "Uøytkownik samodzielnie zmieni≥ has≥o.");

        TempData["SuccessMessage"] = "Has≥o zosta≥o pomyúlnie zmienione.";
        return RedirectToPage("/Index");
    }
}