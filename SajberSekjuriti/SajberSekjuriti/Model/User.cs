using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
namespace SajberSekjuriti.Model
{
    public class User
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }
        public string Username { get; set; }
        public string FullName { get; set; }
        public string PasswordHash { get; set; }
        [BsonRepresentation(BsonType.String)]
        public UserRole Role { get; set; }
        public bool IsBlocked { get; set; } = false;
        public DateTime? PasswordLastSet { get; set; }
        public bool MustChangePassword { get; set; } = false;
    }
    public enum UserRole { Admin, User }
}