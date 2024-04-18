using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace Clio.Command.WebServer;

public class TokenMessageHandler : DelegatingHandler
{

	#region Fields: Private

	private readonly ITokenManager _tokenManager;

	#endregion

	#region Constructors: Public

	public TokenMessageHandler(ITokenManager tokenManager){
		_tokenManager = tokenManager;
	}

	#endregion

	#region Methods: Private

	/// <summary>
	/// Refreshes Token, and repeats the request
	/// </summary>
	/// <param name="environmentNameValue"></param>
	/// <param name="request"></param>
	/// <param name="cancellationToken"></param>
	/// <returns></returns>
	private async Task<HttpResponseMessage> RefreshTokenAndSendAsync(string environmentNameValue,
		HttpRequestMessage request, CancellationToken cancellationToken){
		string freshToken = await _tokenManager.RefreshTokenAsync(environmentNameValue);
		request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", freshToken);
		return await base.SendAsync(request, cancellationToken);
	}

	#endregion

	#region Methods: Protected

	protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
		CancellationToken cancellationToken){
		HttpRequestOptionsKey<string> environmentName = new("environment-name");
		bool isEnvironmentName = request.Options.TryGetValue(environmentName, out string environmentNameValue);
		if (!isEnvironmentName || string.IsNullOrWhiteSpace(environmentNameValue)) {
			return new HttpResponseMessage {
				StatusCode = HttpStatusCode.BadRequest
			};
		}
		string accessToken = _tokenManager.GetToken(environmentNameValue);
		request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
		HttpResponseMessage firstResponse = await base.SendAsync(request, cancellationToken);
		return firstResponse.StatusCode switch {
			HttpStatusCode.Unauthorized => await RefreshTokenAndSendAsync(environmentNameValue, request, cancellationToken),
			var _ => firstResponse
		};
	}

	#endregion

}