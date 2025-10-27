using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc.ViewFeatures; // Potrzebne do TempData
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using SajberSekjuriti.Model;
using SajberSekjuriti.Services;

var builder = WebApplication.CreateBuilder(args);

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

            // --- NOWA LOGIKA: Uruchamiana przy wylogowaniu przez timeout ---
            OnRedirectToLogin = context =>
            {
                // 1. Pobieramy dostêp do TempData
                var tempDataFactory = context.HttpContext.RequestServices.GetRequiredService<ITempDataDictionaryFactory>();
                var tempData = tempDataFactory.GetTempData(context.HttpContext);

                // 2. Czyœcimy wszystkie stare komunikaty
                tempData.Clear();

                // 3. Ustawiamy nowy, poprawny komunikat
                // U¿ywamy klucza "OTPError", aby Login.cshtml.cs go przechwyci³
                tempData["OTPError"] = "Sesja wygas³a. Proszê zalogowaæ siê ponownie.";

                // 4. Kontynuujemy przekierowanie
                context.Response.Redirect(context.RedirectUri);
                return Task.CompletedTask;
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
    }
    await ppService.SaveSettingsAsync(settings);
    Console.WriteLine("Ustawienia polityki hase³ zosta³y utworzone.");
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