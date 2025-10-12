namespace SajberSekjuriti.Services
{
    // Serwis do hashowania i weryfikacji hasel
    public class PasswordService
    {
        // Hashowanie hasla
        public string HashPassword(string password) => BCrypt.Net.BCrypt.HashPassword(password);
        // Weryfikacja hasla
        public bool VerifyPassword(string password, string hash) => BCrypt.Net.BCrypt.Verify(password, hash);
    }
}
