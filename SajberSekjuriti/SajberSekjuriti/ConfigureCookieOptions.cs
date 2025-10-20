using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Options;
using SajberSekjuriti.Services;

public class ConfigureCookieOptions : IConfigureOptions<CookieAuthenticationOptions>
{
    private readonly PasswordPolicyService _policyService;

    public ConfigureCookieOptions(PasswordPolicyService policyService)
    {
        _policyService = policyService;
    }

    public void Configure(CookieAuthenticationOptions options)
    {
        var policySettings = _policyService.GetSettingsAsync().Result;
        var sessionMinutes = policySettings.SessionTimeoutMinutes ?? 0;

        if (sessionMinutes > 0)
        {
            options.ExpireTimeSpan = TimeSpan.FromMinutes(sessionMinutes);
            options.SlidingExpiration = true;
        }
    }
}