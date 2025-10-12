using SajberSekjuriti.Model;
using System.Text.RegularExpressions;

namespace SajberSekjuriti.Services
{
    // Serwis do walidacji hasel zgodnie z polityka
    public class PasswordValidationService
    {
        //Funkcja do sprawdzania hasla zgodnie z polityka
        //Zwraca null jesli haslo jest poprawne
        public string? Validate(string password, PasswordPolicySettings policy)
        {
            if (!policy.IsEnabled)
            {
                return null; 
            }

            if (policy.MinimumLength.HasValue && password.Length < policy.MinimumLength.Value)
            {
                return $"Hasło musi mieć co najmniej {policy.MinimumLength.Value} znaków.";
            }

            if (policy.RequireDigit && !Regex.IsMatch(password, @"\d"))
            {
                return "Hasło musi zawierać co najmniej jedną cyfrę.";
            }

            if (policy.RequireUppercase && !Regex.IsMatch(password, @"[A-Z]"))
            {
                return "Hasło musi zawierać co najmniej jedną wielką literę.";
            }

            if (policy.RequireSpecialCharacter && !Regex.IsMatch(password, @"[\W_]"))
            {
                return "Hasło musi zawierać co najmniej jeden znak specjalny (np. !, @, #).";
            }

            return null;
        }
    }
}