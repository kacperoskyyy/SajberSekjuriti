using MongoDB.Driver;
using SajberSekjuriti.Data; 
using SajberSekjuriti.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();

// Konfiguracja MongoDB
builder.Services.AddSingleton<IMongoClient>(s => new MongoClient(builder.Configuration["MongoDbSettings:ConnectionString"]));
// Nasze serwisy
builder.Services.AddScoped<UsersService>();
builder.Services.AddScoped<PasswordService>();

var app = builder.Build();

// Tworzenie domyœlnego admina
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var usersService = services.GetRequiredService<UsersService>();
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

app.UseAuthorization();

app.MapRazorPages();

app.Run();
