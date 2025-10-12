using Microsoft.AspNetCore.Authentication.Cookies;
using MongoDB.Driver;
using SajberSekjuriti.Model; 
using SajberSekjuriti.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Login";
        options.AccessDeniedPath = "/AccessDenied";
    });

// Add services to the container.
builder.Services.AddRazorPages();

// Konfiguracja MongoDB
builder.Services.AddSingleton<IMongoClient>(s => new MongoClient(builder.Configuration["MongoDbSettings:ConnectionString"]));
// Wstrzykniecie serwisów 
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<PasswordService>();
builder.Services.AddScoped<PasswordPolicyService>();
builder.Services.AddScoped<PasswordValidationService>();

var app = builder.Build();

// Tworzenie domyœlnego admina
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
            MustChangePassword = true
        };
        await usersService.CreateAsync(newAdmin);
        Console.WriteLine("Stworzono konto admina.");
    }
}

// Tworzenie domyœlnych ustawieñ polityki hase³

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

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();

app.Run();
