using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using MongoDB.Driver;
using SajberSekjuriti.Model;
using SajberSekjuriti.Services;

var builder = WebApplication.CreateBuilder(args);

// Register code pages encoding provider for non-UTF8 encodings (e.g., ISO-8859-1)
System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Login";
        options.AccessDeniedPath = "/AccessDenied";

        options.SlidingExpiration = true;

        options.Events = new CookieAuthenticationEvents
        {
            OnSigningIn = async context =>
            {
                var policyService = context.HttpContext.RequestServices.GetRequiredService<PasswordPolicyService>();
                var policy = await policyService.GetSettingsAsync();

                if (policy.SessionTimeoutMinutes.HasValue && policy.SessionTimeoutMinutes.Value > 0)
                {
                    context.Properties.ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(policy.SessionTimeoutMinutes.Value);
                    context.Properties.AllowRefresh = true;
                }
            },

            OnValidatePrincipal = async context =>
            {
                var userService = context.HttpContext.RequestServices.GetRequiredService<UserService>();
                var userClaim = context.Principal?.Identity?.Name;

                if (userClaim == null)
                {
                    context.RejectPrincipal();
                    await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                    return;
                }

                var user = await userService.GetByUsernameAsync(userClaim);

                if (user == null)
                {
                    context.RejectPrincipal();
                    await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                    return;
                }

                if (user.MustChangePassword)
                {
                    if (!context.Request.Path.StartsWithSegments("/ChangePassword") &&
                        !context.Request.Path.StartsWithSegments("/Logout"))
                    {
                        context.Response.Redirect("/ChangePassword");
                        return;
                    }
                }
            },

            OnRedirectToLogin = async context =>
            {
                var tempDataFactory = context.HttpContext.RequestServices.GetRequiredService<ITempDataDictionaryFactory>();
                var tempData = tempDataFactory.GetTempData(context.HttpContext);
                var _auditLogService = context.HttpContext.RequestServices.GetRequiredService<AuditLogService>();

                tempData.Clear();
                tempData["OTPError"] = "Sesja wygas³a. Proszê zalogowaæ siê ponownie.";

                context.Response.Redirect(context.RedirectUri);

                // Avoid blocking/throwing here
                try
                {
                    await _auditLogService.LogAsync("Nieznany", "Wygaœniêcie sesji", "Sesja u¿ytkownika wygas³a podczas logowania OTP.");
                }
                catch
                {
                    // Swallow logging failures to prevent process crash
                }
            }
        };
    });

builder.Services.AddRazorPages();

builder.Services.AddSingleton<IMongoClient>(s => new MongoClient(builder.Configuration["MongoDbSettings:ConnectionString"]));
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<PasswordService>();
builder.Services.AddScoped<AuditLogService>();
builder.Services.AddSingleton<PasswordPolicyService>();
builder.Services.AddScoped<PasswordValidationService>();
builder.Services.AddHttpClient<ReCaptchaService>();
builder.Services.AddScoped<VigenereCipherService>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var usersService = services.GetRequiredService<UserService>();
    var passwordService = services.GetRequiredService<PasswordService>();
    var adminUser = await usersService.GetByUsernameAsync("ADMIN");
    if (adminUser == null)
    {
        var newAdmin = new User
        {
            Username = "ADMIN",
            FullName = "Administrator",
            PasswordHash = passwordService.HashPassword("Admin123!"),
            Role = UserRole.Admin,
            PasswordLastSet = DateTime.UtcNow,
            MustChangePassword = false
        };
        await usersService.CreateAsync(newAdmin);
        Console.WriteLine("Stworzono konto admina.");
    }
}

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var ppService = services.GetRequiredService<PasswordPolicyService>();
    var settings = await ppService.GetSettingsAsync();
    if (settings == null)
    {
        var defaultSettings = new PasswordPolicySettings
        {
            Id = null,
            IsEnabled = false,
            RequireDigit = true,
            RequireSpecialCharacter = true,
            RequireUppercase = true,
            MinimumLength = 8,
            PasswordExpirationDays = 0
        };
        await ppService.SaveSettingsAsync(defaultSettings);
        Console.WriteLine("Ustawienia polityki hase³ zosta³y utworzone.");
    }
    else
    {
        // Ensure existing settings are persisted/normalized as needed
        await ppService.SaveSettingsAsync(settings);
    }
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();

app.Run();