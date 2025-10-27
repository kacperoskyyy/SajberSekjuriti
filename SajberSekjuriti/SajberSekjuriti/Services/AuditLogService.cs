using MongoDB.Driver;
using MongoDB.Bson;
using SajberSekjuriti.Model;

namespace SajberSekjuriti.Services;

public class PaginatedLogResult
{
    public List<AuditLog> Logs { get; set; } = new List<AuditLog>();
    public long TotalItems { get; set; }
}

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

    public async Task<PaginatedLogResult> GetPaginatedFilteredLogsAsync(
        string? searchUsername,
        string? searchAction,
        DateTime? startDate,
        DateTime? endDate,
        int currentPage,
        int pageSize)
    {
        var filterBuilder = Builders<AuditLog>.Filter;
        var filter = filterBuilder.Empty;

        if (!string.IsNullOrEmpty(searchUsername))
        {
            filter &= filterBuilder.Regex(log => log.Username, new BsonRegularExpression(searchUsername, "i"));
        }
        if (!string.IsNullOrEmpty(searchAction))
        {
            filter &= filterBuilder.Eq(log => log.Action, searchAction);
        }
        if (startDate.HasValue)
        {
            filter &= filterBuilder.Gte(log => log.Timestamp, startDate.Value);
        }
        if (endDate.HasValue)
        {
            filter &= filterBuilder.Lt(log => log.Timestamp, endDate.Value.AddDays(1));
        }

        var totalItems = await _logsCollection.CountDocumentsAsync(filter);

        var logs = await _logsCollection.Find(filter)
            .SortByDescending(log => log.Timestamp)
            .Skip((currentPage - 1) * pageSize)
            .Limit(pageSize)
            .ToListAsync();

        return new PaginatedLogResult { Logs = logs, TotalItems = totalItems };
    }

    public async Task<List<string>> GetDistinctActionsAsync()
    {
        return await _logsCollection.Distinct(log => log.Action, Builders<AuditLog>.Filter.Empty).ToListAsync();
    }
}