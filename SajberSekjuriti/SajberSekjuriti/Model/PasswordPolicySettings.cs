using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.ComponentModel.DataAnnotations;

namespace SajberSekjuriti.Model
{
    public class PasswordPolicySettings
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = string.Empty;

        [Display(Name = "Włącz własne reguły złożoności hasła")]
        public bool IsEnabled { get; set; } = false;

        [Display(Name = "Wymagaj cyfry")]
        public bool RequireDigit { get; set; } = false;

        [Display(Name = "Wymagaj znaku specjalnego")]
        public bool RequireSpecialCharacter { get; set; } = false;

        [Display(Name = "Wymagaj wielkiej litery")]
        public bool RequireUppercase { get; set; } = false;

        [Required(ErrorMessage = "Minimalna długość jest wymagana.")]
        [Display(Name = "Minimalna długość hasła")]
        public string MinimumLength { get; set; } = "8";

        [Required(ErrorMessage = "Ważność hasła jest wymagana.")]
        [Display(Name = "Ważność hasła (w dniach, 0 = wyłączone)")]
        public string PasswordExpirationDays { get; set; } = "0";
    }
}