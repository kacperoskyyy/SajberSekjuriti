using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace SajberSekjuriti.Model;

public class AuditLog
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = string.Empty;

    [BsonElement("Username")]
    public string Username { get; set; } = string.Empty;

    [BsonElement("Timestamp")]
    public DateTime Timestamp { get; set; }

    [BsonElement("Action")]
    public string Action { get; set; } = string.Empty;

    [BsonElement("Description")]
    public string Description { get; set; } = string.Empty;
}