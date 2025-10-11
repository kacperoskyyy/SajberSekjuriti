using SajberSekjuriti.Model;
using System.Text.RegularExpressions;

namespace SajberSekjuriti.Services
{
    public class PasswordValidationService
    {
        public string? Validate(string password, PasswordPolicySettings policy)
        {
            if (!policy.IsEnabled)
            {
                return null;
            }

            if (password.Length < policy.MinimumLength.Value)
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