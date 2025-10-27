using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SajberSekjuriti.Services;
using System.ComponentModel.DataAnnotations;

namespace SajberSekjuriti.Pages;

[Authorize]
public class ChangePasswordModel : PageModel
{
    private readonly UserService _userService;
    private readonly PasswordService _passwordService;
    private readonly PasswordPolicyService _policyService;
    private readonly PasswordValidationService _validationService;
    private readonly AuditLogService _auditLogService;
    private readonly ReCaptchaService _reCaptchaService;
    private readonly IConfiguration _configuration;

    public ChangePasswordModel(
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

    public string? ErrorMessage { get; set; }

    public IConfiguration Configuration => _configuration;

    public class InputModel
    {
        [Required(ErrorMessage = "Stare has³o jest wymagane")]
        [DataType(DataType.Password)]
        [Display(Name = "Stare has³o")]
        public string OldPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Nowe has³o jest wymagane")]
        [StringLength(100, ErrorMessage = "Has³o musi mieæ co najmniej 6 znaków.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "Nowe has³o")]
        public string NewPassword { get; set; } = string.Empty;

        [DataType(DataType.Password)]
        [Display(Name = "PotwierdŸ nowe has³o")]
        [Compare("NewPassword", ErrorMessage = "Has³a nie s¹ takie same.")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var reCaptchaToken = Request.Form["g-recaptcha-response"];
        var isReCaptchaValid = await _reCaptchaService.ValidateAsync(reCaptchaToken);

        if (!isReCaptchaValid)
        {
            ErrorMessage = "Weryfikacja CAPTCHA nie powiod³a siê. Spróbuj ponownie.";
            return Page();
        }

        if (!ModelState.IsValid)
        {
            return Page();
        }

        var policy = await _policyService.GetSettingsAsync();
        var validationError = _validationService.Validate(Input.NewPassword, policy);
        if (validationError != null)
        {
            ModelState.AddModelError(string.Empty, validationError);
            return Page();
        }

        var username = User.Identity?.Name;
        if (username == null)
        {
            return RedirectToPage("/Login");
        }
        var user = await _userService.GetByUsernameAsync(username);
        if (user == null || !_passwordService.VerifyPassword(Input.OldPassword, user.PasswordHash))
        {
            ModelState.AddModelError("Input.OldPassword", "Stare has³o jest nieprawid³owe.");
            return Page();
        }

        foreach (var oldHash in user.PasswordHistory)
        {
            if (_passwordService.VerifyPassword(Input.NewPassword, oldHash))
            {
                ModelState.AddModelError("Input.NewPassword", "Nowe has³o nie mo¿e byæ takie samo jak jedno z 5 poprzednich.");
                return Page();
            }
        }

        user.PasswordHistory.Add(user.PasswordHash);

        while (user.PasswordHistory.Count > 5)
        {
            user.PasswordHistory.RemoveAt(0);
        }

        user.PasswordHash = _passwordService.HashPassword(Input.NewPassword);
        user.MustChangePassword = false;
        user.PasswordLastSet = DateTime.UtcNow;

        await _userService.UpdateAsync(user);
        await _auditLogService.LogAsync(username, "Zmiana has³a", "U¿ytkownik zosta³ zmuszony do zmiany has³a.");

        return RedirectToPage("/Index");
    }
}