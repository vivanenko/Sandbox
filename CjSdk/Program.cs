using System.Net.Http.Json;
using System.Text.Json.Serialization;

Console.WriteLine("Hello, World!");

public sealed record CjToken(AccessToken AccessToken, RefreshToken RefreshToken);
public sealed record AccessToken(string Value, DateTime ExpiresAt)
{
    public bool IsExpired => DateTime.UtcNow > ExpiresAt;
}
public sealed record RefreshToken(string Value, DateTime ExpiresAt)
{
    public bool IsExpired => DateTime.UtcNow > ExpiresAt;
}

public interface ICjTokenStore
{
    Task<CjToken> GetTokenAsync(CancellationToken cancellationToken);
    Task SaveTokenAsync(CjToken token, CancellationToken cancellationToken);
}

public sealed class CjClient
{
    private sealed class AuthResponse
    {
        [JsonPropertyName("code")]
        public int Code { get; set; }

        [JsonPropertyName("result")]
        public bool Result { get; set; }

        [JsonPropertyName("message")]
        public string Message { get; set; }

        [JsonPropertyName("data")]
        public TokenData Data { get; set; }

        [JsonPropertyName("requestId")]
        public string RequestId { get; set; }
    }
    private sealed class TokenData
    {
        [JsonPropertyName("accessToken")]
        public string AccessToken { get; set; }

        [JsonPropertyName("accessTokenExpiryDate")]
        public DateTimeOffset AccessTokenExpiryDate { get; set; }

        [JsonPropertyName("refreshToken")]
        public string RefreshToken { get; set; }

        [JsonPropertyName("refreshTokenExpiryDate")]
        public DateTimeOffset RefreshTokenExpiryDate { get; set; }

        [JsonPropertyName("createDate")]
        public DateTimeOffset CreateDate { get; set; }
    }
    
    private readonly string _email;
    private readonly string _password;
    private readonly ICjTokenStore _tokenStore;
    private readonly IHttpClientFactory _httpClientFactory;
    private CjToken? _token;

    public CjClient(string email, string password, ICjTokenStore tokenStore, IHttpClientFactory httpClientFactory)
    {
        _email = email;
        _password = password;
        _tokenStore = tokenStore;
        _httpClientFactory = httpClientFactory;
    }

    private async Task EnsureAuthenticatedAsync(CancellationToken cancellationToken)
    {
        if (_token == null || _token.RefreshToken.IsExpired)
        {
            await AuthenticateAsync(cancellationToken);
        }
        else if (_token.AccessToken.IsExpired)
        {
            await RefreshTokenAsync(cancellationToken);
        }
    }

    private async Task AuthenticateAsync(CancellationToken cancellationToken)
    {
        var httpClient = _httpClientFactory.CreateClient();
        var response = await httpClient.PostAsJsonAsync(
            "https://developers.cjdropshipping.com/api2.0/v1/authentication/getAccessToken",
            new
            {
                email = _email,
                password = _password
            }, cancellationToken);

        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<AuthResponse>(cancellationToken);
        if (result is null) throw new InvalidOperationException("Could not parse response");

        var accessToken = new AccessToken(result.Data.AccessToken, result.Data.AccessTokenExpiryDate.UtcDateTime);
        var refreshToken = new RefreshToken(result.Data.RefreshToken, result.Data.RefreshTokenExpiryDate.UtcDateTime);
        _token = new CjToken(accessToken, refreshToken);
        // Save token
    }

    private async Task RefreshTokenAsync(CancellationToken cancellationToken)
    {
        
    }

    public async Task<string> GetProductsAsync(CancellationToken cancellationToken)
    {
        await EnsureAuthenticatedAsync(cancellationToken);
        
        return "";
    }
}