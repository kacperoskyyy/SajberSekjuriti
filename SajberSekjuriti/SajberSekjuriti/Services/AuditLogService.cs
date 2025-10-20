using MongoDB.Driver;
using SajberSekjuriti.Model;
using SajberSekjuriti.Pages;

namespace SajberSekjuriti.Services
{
    public class AuditLogService
    {
        private readonly IMongoCollection<AuditLog> _logsCollection;
        private readonly PasswordPolicyService _policyService;

        public AuditLogService(IMongoClient client, IConfiguration config, PasswordPolicyService policyService)
        {
            _policyService = policyService;
            var dbName = config["MongoDbSettings:DatabaseName"];
            var collectionName = "audit_logs";
            var database = client.GetDatabase(dbName);
            _logsCollection = database.GetCollection<AuditLog>(collectionName);
        }


        public async Task LogAsync(string username, string action, string description)
        {
            var policy = await _policyService.GetSettingsAsync();

            if (!policy.EnableAuditLog)
            {
                return;
            }

            var logEntry = new AuditLog
            {
                Username = username,
                Timestamp = DateTime.UtcNow,
                Action = action,
                Description = description
            };

            await _logsCollection.InsertOneAsync(logEntry);
        }

        public async Task<List<AuditLog>> GetAllLogsAsync()
        {
            return await _logsCollection.Find(_ => true)
                                        .SortByDescending(log => log.Timestamp)
                                        .ToListAsync();
        }

        public static implicit operator AuditLogService(AuditLogModel v)
        {
            throw new NotImplementedException();
        }
    }
}