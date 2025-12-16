using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;

public class ReCaptchaService
{
    private readonly IConfiguration _config;
    private readonly HttpClient _httpClient;

    public ReCaptchaService(IConfiguration config)
    {
        _config = config;
        _httpClient = new HttpClient();
    }

    public async Task<bool> Verify(string token)
    {
        var secret = _config["GoogleReCaptcha:SecretKey"];

        var response = await _httpClient.PostAsync(
            $"https://www.google.com/recaptcha/api/siteverify?secret={secret}&response={token}",
            null
        );

        var json = await response.Content.ReadAsStringAsync();
        dynamic result = JsonConvert.DeserializeObject(json);

        return result.success == true;
    }
}
