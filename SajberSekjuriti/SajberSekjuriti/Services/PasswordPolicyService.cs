using MongoDB.Driver;
using SajberSekjuriti.Model;

namespace SajberSekjuriti.Services;

public class PasswordPolicyService
{
    private readonly IMongoCollection<PasswordPolicySettings> _settingsCollection;
    public PasswordPolicyService(IMongoClient client, IConfiguration config)
    {
        var dbName = config["MongoDbSettings:DatabaseName"];
        var collectionName = "password_policy_settings";
        var database = client.GetDatabase(dbName);
        _settingsCollection = database.GetCollection<PasswordPolicySettings>(collectionName);
    }
    public async Task<PasswordPolicySettings> GetSettingsAsync()
    {
        var settings = await _settingsCollection.Find(_ => true).FirstOrDefaultAsync();
        if (settings == null)
        {
            return new PasswordPolicySettings();
        }
        return settings;
    }
    public async Task SaveSettingsAsync(PasswordPolicySettings settings)
    {
        try
        {
            var existing = await _settingsCollection.Find(_ => true).FirstOrDefaultAsync();

            if (existing == null)
            {
                await _settingsCollection.InsertOneAsync(settings);
            }
            else
            {
                settings.Id = existing.Id;
                await _settingsCollection.ReplaceOneAsync(s => s.Id == existing.Id, settings);
            }
        }
        catch (Exception ex)
        {
            throw;
        }
    }
}