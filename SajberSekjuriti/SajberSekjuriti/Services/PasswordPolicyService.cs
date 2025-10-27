using MongoDB.Driver;
using SajberSekjuriti.Model;

namespace SajberSekjuriti.Services;

public class PasswordPolicyService
{
    private readonly IMongoCollection<PasswordPolicySettings> _settingsCollection;
    private readonly ILogger<PasswordPolicyService> _logger;
    public PasswordPolicyService(IMongoClient client, IConfiguration config, ILogger<PasswordPolicyService> logger)
    {
        _logger = logger;
        var dbName = config["MongoDbSettings:DatabaseName"];
        var collectionName = "password_policy_settings";
        var database = client.GetDatabase(dbName);
        _settingsCollection = database.GetCollection<PasswordPolicySettings>(collectionName);
        _logger.LogInformation("PasswordPolicyService zainicjalizowany.");
    }
    public async Task<PasswordPolicySettings> GetSettingsAsync()
    {
        _logger.LogInformation("Pobieram ustawienia polityki haseł...");
        var settings = await _settingsCollection.Find(_ => true).FirstOrDefaultAsync();
        if (settings == null)
        {
            _logger.LogWarning("Nie znaleziono ustawień w bazie. Zwracam nowe, domyślne.");
            return new PasswordPolicySettings();
        }
        _logger.LogInformation("Znaleziono ustawienia w bazie. ID: {SettingsId}", settings.Id);
        return settings;
    }
    public async Task SaveSettingsAsync(PasswordPolicySettings settings)
    {
        try
        {
            _logger.LogInformation("Próba zapisu ustawień...");
            var existing = await _settingsCollection.Find(_ => true).FirstOrDefaultAsync();

            if (existing == null)
            {
                _logger.LogInformation("Baza jest pusta. Tworzę nowy dokument...");
                await _settingsCollection.InsertOneAsync(settings);
                _logger.LogInformation("NOWY DOKUMENT UTWORZONY. ID: {SettingsId}", settings.Id);
            }
            else
            {
                _logger.LogInformation("Znaleziono istniejący dokument (ID: {ExistingId}). Aktualizuję go.", existing.Id);
                settings.Id = existing.Id;
                await _settingsCollection.ReplaceOneAsync(s => s.Id == existing.Id, settings);
                _logger.LogInformation("DOKUMENT ZAKTUALIZOWANY.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "KRYTYCZNY BŁĄD podczas zapisu polityki haseł!");
            throw;
        }
    }
}