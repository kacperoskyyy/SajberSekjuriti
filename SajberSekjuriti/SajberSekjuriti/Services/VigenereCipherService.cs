namespace SajberSekjuriti.Services
{
    public class VigenereCipherService
    {
        private string NormalizeText(string text)
        {
            return new string(text.ToUpper().Where(char.IsLetter).ToArray());
        }

        private char EncryptChar(char plain, char key)
        {
            if (!char.IsLetter(plain) || !char.IsLetter(key)) return plain;
            int plainOffset = char.IsUpper(plain) ? 'A' : 'a';
            int keyOffset = char.IsUpper(key) ? 'A' : 'a';
            int encrypted = (plain - plainOffset + (key - keyOffset)) % 26;
            return (char)(encrypted + plainOffset);
        }

        private char DecryptChar(char cipher, char key)
        {
            if (!char.IsLetter(cipher) || !char.IsLetter(key)) return cipher;
            int cipherOffset = char.IsUpper(cipher) ? 'A' : 'a';
            int keyOffset = char.IsUpper(key) ? 'A' : 'a';
            int decrypted = (cipher - cipherOffset - (key - keyOffset) + 26) % 26;
            return (char)(decrypted + cipherOffset);
        }

        // --- POCZĄTEK DODANEJ METODY ---

        public string Encrypt(string plainText, string key)
        {
            if (string.IsNullOrEmpty(key)) return plainText;

            string normalizedKey = NormalizeText(key);
            string normalizedText = NormalizeText(plainText);
            string encrypted = "";
            int keyIndex = 0;

            for (int i = 0; i < normalizedText.Length; i++)
            {
                encrypted += EncryptChar(normalizedText[i], normalizedKey[keyIndex]);
                keyIndex = (keyIndex + 1) % normalizedKey.Length;
            }
            return encrypted;
        }

        // --- KONIEC DODANEJ METODY ---

        public string Decrypt(string cipherText, string key)
        {
            if (string.IsNullOrEmpty(key)) return cipherText;

            string normalizedKey = NormalizeText(key);
            string normalizedText = NormalizeText(cipherText);
            string decrypted = "";
            int keyIndex = 0;

            for (int i = 0; i < normalizedText.Length; i++)
            {
                decrypted += DecryptChar(normalizedText[i], normalizedKey[keyIndex]);
                keyIndex = (keyIndex + 1) % normalizedKey.Length;
            }
            return decrypted;
        }
    }
}