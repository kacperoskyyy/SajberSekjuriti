using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.ComponentModel.DataAnnotations;

namespace SajberSekjuriti.Model
{
    // Model reprezentujący ustawienia polityki haseł
    public class PasswordPolicySettings
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; } = string.Empty;

        [Display(Name = "Włącz własne reguły złożoności hasła")]
        public bool IsEnabled { get; set; } = false;

        [Display(Name = "Minimalna długość hasła")]
        public int? MinimumLength { get; set; } = 8;

        [Display(Name = "Wymagaj cyfry")]
        public bool RequireDigit { get; set; } = false;

        [Display(Name = "Wymagaj znaku specjalnego")]
        public bool RequireSpecialCharacter { get; set; } = false;

        [Display(Name = "Wymagaj wielkiej litery")]
        public bool RequireUppercase { get; set; } = false;

        [Display(Name = "Ważność hasła (w dniach, 0 = wyłączone)")]
        public int? PasswordExpirationDays { get; set; } = 0;
        [Display(Name = "Włącz zapisywanie logów aktywności")]
        public bool EnableAuditLog { get; set; } = true;

        [Display(Name = "Limit błędnych logowań (0 = wyłączone)")]
        public int? MaxLoginAttempts { get; set; } = 5;

        [Display(Name = "Czas blokady konta (w minutach)")]
        public int? LockoutDurationMinutes { get; set; } = 15;

        [Display(Name = "Czas sesji użytkownika (w minutach, 0 = bez limitu)")]
        public int? SessionTimeoutMinutes { get; set; } = 10;
    }
}