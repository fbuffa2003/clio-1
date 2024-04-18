using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Microsoft.Extensions.Logging;

namespace Clio.Command.WebServer;

public class LimiterHandler : DelegatingHandler
{

	#region Constants: Private

	private const uint Top = 5;

	#endregion

	#region Fields: Private

	private readonly ILogger<LimiterHandler> _logger;

	#endregion

	#region Constructors: Public

	public LimiterHandler(ILogger<LimiterHandler> logger){
		_logger = logger;
	}

	#endregion

	#region Methods: Private

	private static Uri CreateUriWithParameter(Uri baseUri, string parameter){
		Uri.TryCreate(baseUri, parameter, out Uri result);
		return result;
	}

	private static string GetEntityFromRequestString(Uri requestUri) =>
		requestUri.LocalPath.Replace("/0/odata", "").Replace("/", "");

	private static bool HasTop(Uri requestUri) =>
		requestUri.Query.ToLower(CultureInfo.InstalledUICulture).Contains("$top=");

	private static string ToQueryString(NameValueCollection nvc){
		List<string> array = new();
		foreach (string key in nvc.AllKeys) {
			foreach (string value in nvc.GetValues(key)) {
				array.Add(string.Concat(key, "=", value));
			}
		}
		return string.Join("&", array);
	}

	private HttpRequestMessage UpdateRequestUri(HttpRequestMessage request){
		NameValueCollection queryParams = HttpUtility
			.ParseQueryString(request.RequestUri!.Query);
		if (queryParams.AllKeys.Contains("$top")) {
			int.TryParse(queryParams["$top"], out int i);
			queryParams["$top"] = i <= Top ? queryParams["$top"] : Top.ToString();
		} else {
			queryParams.Add("$top", Top.ToString());
		}
		string queryString = ToQueryString(queryParams);
		_logger.LogInformation("Adjusted top to: {0}, new query:{1}", queryParams["$top"], queryString);
		request.RequestUri = CreateUriWithParameter(request.RequestUri, "?" + queryString);
		return request;
		
	}

	#endregion

	#region Methods: Protected

	protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
		CancellationToken cancellationToken){
		_logger.LogInformation("Entered SendAsync");
		Uri u = request.RequestUri;
		Uri requestUri = request.RequestUri;
		bool top = HasTop(requestUri);
		string entityName = GetEntityFromRequestString(requestUri);

		return entityName switch {
			_ when !string.IsNullOrWhiteSpace(entityName) && !top =>
				base.SendAsync(UpdateRequestUri(request), cancellationToken),
			_ =>
				base.SendAsync(request, cancellationToken)
		};
	}

	#endregion

	private record ODataResponse([property: JsonPropertyName("@odata.context")]
		string Context,
		[property: JsonPropertyName("value")] List<object> Records);

}