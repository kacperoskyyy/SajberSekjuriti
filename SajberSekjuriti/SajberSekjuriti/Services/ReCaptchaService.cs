using System.Text.Json.Serialization;

namespace SajberSekjuriti.Services;

public class ReCaptchaResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }
    [JsonPropertyName("error-codes")]
    public List<string>? ErrorCodes { get; set; }
}

public class ReCaptchaService
{
    private readonly HttpClient _httpClient;
    private readonly string _secretKey;

    public ReCaptchaService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _secretKey = configuration["ReCaptchaSettings:SecretKey"];
    }

    public async Task<bool> ValidateAsync(string token)
    {
        if (string.IsNullOrEmpty(token))
        {
            return false;
        }

        try
        {
            var response = await _httpClient.PostAsync(
                $"https://www.google.com/recaptcha/api/siteverify?secret={_secretKey}&response={token}",
                null);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<ReCaptchaResponse>();
                return result?.Success ?? false;
            }
        }
        catch (Exception)
        {
            
            return false;
        }

        return false;
    }
}