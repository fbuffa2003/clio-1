using System;
using System.Collections.Generic;
using System.DirectoryServices;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Clio.UserEnvironment;
using Microsoft.Extensions.Logging;

namespace Clio.Command.WebServer;

public interface ITokenManager
{

	public string GetToken(string environmentName);
	public void SetToken(string environmentName, string tokenValue);
	public Task<string> RefreshTokenAsync(string environmentName);

}

public class TokenManager : ITokenManager
{

	private readonly IHttpClientFactory _httpclientFactory;
	private readonly ISettingsRepository _settingsRepository;
	private readonly ILogger<TokenManager> _logger;
	private readonly Dictionary<string, string> _tokensDictionary = new();
	private readonly JsonSerializerOptions _jsonSerializerOptions = new JsonSerializerOptions{
		AllowTrailingCommas = true,
	};
	public TokenManager(IHttpClientFactory httpclientFactory, ISettingsRepository settingsRepository, ILogger<TokenManager> logger){
		_httpclientFactory = httpclientFactory;
		_settingsRepository = settingsRepository;
		_logger = logger;
	}

	public string GetToken(string environmentName){
		_logger.LogInformation("Getting token");
		return _tokensDictionary.TryGetValue(environmentName, out string token) ? token: string.Empty;
	}

	public void SetToken(string environmentName, string tokenValue){
		if(_tokensDictionary.ContainsKey(environmentName)) {
			_logger.LogInformation("Setting new  token");
			_tokensDictionary[environmentName] = tokenValue;
		}else {
			_logger.LogInformation("Updating token");
			_tokensDictionary.TryAdd(environmentName, tokenValue);
		}
	}

	public async Task<string> RefreshTokenAsync(string environmentName){
		_logger.LogInformation("Refreshing token");
		HttpClient httpClient = _httpclientFactory.CreateClient("IdentityClient");
		EnvironmentSettings environment = _settingsRepository.GetEnvironment(environmentName);
		
		Uri.TryCreate(environment.AuthAppUri, UriKind.Absolute, out Uri uriValue);
		httpClient.BaseAddress = uriValue;
		
		IEnumerable<KeyValuePair<string, string>> keys = new List<KeyValuePair<string, string>>() {
			new ("client_Id",environment.ClientId),
			new ("client_secret",environment.ClientSecret),
			new ("grant_type","client_credentials")
		};
		var formUrlEncodedContent = new FormUrlEncodedContent(keys);
		HttpRequestMessage requestMessage = new () {
			Content = formUrlEncodedContent,
			Method = HttpMethod.Post
		};
		HttpResponseMessage responseMessage = await httpClient.SendAsync(requestMessage);
		Stream stream = await responseMessage.Content.ReadAsStreamAsync();
		TokenResponse tokenModel = await JsonSerializer.DeserializeAsync<TokenResponse>(stream, _jsonSerializerOptions);
		
		if(tokenModel is not null && !string.IsNullOrWhiteSpace(tokenModel.AccessToken)) {
			SetToken(environmentName, tokenModel.AccessToken);
		}
		
		return GetToken(environmentName);
	}

	public record TokenResponse(
		[property: JsonPropertyName("access_token")] string AccessToken, 
		[property: JsonPropertyName("token_type")] string TokenType, 
		[property: JsonPropertyName("scope")] string Scope, 
		[property: JsonPropertyName("expires_in")] uint ExpiresIn,
		[property: JsonPropertyName("error")] string Error
		
		);
}