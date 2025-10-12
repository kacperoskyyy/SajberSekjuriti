using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.ComponentModel.DataAnnotations;
namespace SajberSekjuriti.Model
{
    // Model reprezentujący użytkownika w systemie
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
        [Display(Name = "Indywidualna ważność hasła (w dniach, puste = globalna)")]
        public int? PasswordExpirationDays { get; set; }
        public List<string> PasswordHistory { get; set; } = new List<string>();
    }
    // Enum definiujący role użytkowników
    public enum UserRole { Admin, User }
}