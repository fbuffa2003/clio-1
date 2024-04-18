using System;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Clio.Command.WebServer;
using Clio.UserEnvironment;
using CommandLine;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using Polly.Extensions.Http;
using HttpVersion = System.Net.HttpVersion;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace Clio.Command;

[Verb("start-server", Aliases = new[] {"ss"}, HelpText = "Start web server")]
public class StartServerOptions : EnvironmentNameOptions
{

	#region Properties: Public

	[Option("Port", Required = false, HelpText = "Default server port", Default = 19_999)]
	public int Port { get; set; }

	#endregion

}

public class StartServerCommand : Command<StartServerOptions>
{

	#region Fields: Private

	private readonly ISettingsRepository _settingsRepository;
	private readonly Microsoft.AspNetCore.Builder.WebApplication _app;

	#endregion

	#region Constructors: Public

	public StartServerCommand(ISettingsRepository settingsRepository){
		_settingsRepository = settingsRepository;

		WebApplicationBuilder builder = Microsoft.AspNetCore.Builder.WebApplication.CreateBuilder();
		builder.Services.AddCors(options => {
			options.AddPolicy("CorsPolicy",
				policyBuilder => { policyBuilder.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader(); });
		});
		builder.Services.AddSingleton(_settingsRepository);
		builder.Services.AddSingleton<ITokenManager, TokenManager>();
		builder.Services.AddHttpClient("IdentityClient", client => {
			client.DefaultRequestVersion = HttpVersion.Version20;
			client.DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrLower;
		});

		builder.Services.AddTransient<TokenMessageHandler>();
		builder.Services.AddTransient<LimiterHandler>();

		builder.Services.AddHttpClient("CreatioClient", client => {
				client.DefaultRequestVersion = HttpVersion.Version20;
				client.DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrLower;
				client.Timeout = TimeSpan.FromSeconds(1_000);
			})
			.AddHttpMessageHandler<LimiterHandler>()
			.AddHttpMessageHandler<TokenMessageHandler>()
			.AddPolicyHandler(GetRetryPolicy())
			.ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler {
				AllowAutoRedirect = true,
				UseDefaultCredentials = false
			});

		_app = builder.Build();
		_app.UseCors("CorsPolicy");
		_app.MapMethods(
			"/proxy/{env}/{*proxyString}",
			new[] {
				HttpMethods.Get
				// HttpMethods.Delete,
				// HttpMethods.Head,
				// HttpMethods.Options,
				// HttpMethods.Trace,
				// HttpMethods.Connect
			},
			async ([FromServices] IHttpClientFactory httpClientFactory, HttpContext context, string env,
					string proxyString) =>
				await ProxyHandlerHandler(httpClientFactory, context, env, proxyString)
		);
	}

	#endregion

	#region Methods: Private

	private static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy(){
		return HttpPolicyExtensions
			.HandleTransientHttpError() // Handles HttpRequestException, 5XX and 408
			.Or<SocketException>() // Handles SocketException
			.WaitAndRetryAsync(3, retryAttempt =>
					TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
				(outcome, timespan, retryAttempt, context) => {
					// This is a good place to log the details of the retry
					Console.WriteLine(
						$"Retrying due to: {outcome.Exception?.Message ?? "No exception message."}. Retry attempt: {retryAttempt}. Waiting {timespan} before next retry.");
				});
	}

	/// <summary>
	///     Proxies request
	/// </summary>
	/// <param name="httpClientFactory"></param>
	/// <param name="context">HTTP Context</param>
	/// <param name="env">Environment Name where we take base url from</param>
	/// <param name="proxyString">Partial URL of the destination</param>
	/// <returns></returns>
	private async Task<IResult> ProxyHandlerHandler(IHttpClientFactory httpClientFactory, HttpContext context,
		string env, string proxyString){
		EnvironmentSettings environment = _settingsRepository.GetEnvironment(env);
		bool isUri = Uri.TryCreate(environment.Uri, UriKind.Absolute, out Uri baseUri);
		if (!isUri) {
			return Results.BadRequest(new {
				Error = $"Environment with key {env} does not have correct Uri: {environment.Uri}",
				EnvironmentName = env
			});
		}
		HttpClient client = httpClientFactory.CreateClient("CreatioClient");
		client.BaseAddress = baseUri;

		HttpRequestMessage httpRequestMessage = new();
		HttpRequestOptionsKey<string> environmentName = new("environment-name");
		httpRequestMessage.Options.Set(environmentName, env);
		Uri.TryCreate(baseUri, proxyString + context.Request.QueryString.Value, out Uri requestUri);
		httpRequestMessage.RequestUri = requestUri;
		httpRequestMessage.Method = new HttpMethod(context.Request.Method);
		HttpResponseMessage response = await client.SendAsync(httpRequestMessage);

		//OData specific stuff
		context.Response.Headers.Add("OData-Version", "4.0");
		context.Response.Headers.Add("Pragma", "no-cache");
		context.Response.Headers.Add("Cache-Control", "no-cache");
		context.Response.Headers.Add("Expires", "-1");
		context.Response.Headers.Add("Prefer", "odata.maxpagesize=100");

		string stringContent = await response.Content.ReadAsStringAsync();
		string returnContent = stringContent.Replace(environment.Uri!, $"http://127.0.0.1:19999/proxy/{env}");

		return response.StatusCode switch {
			HttpStatusCode.NoContent => Results.NoContent(),
			_ => Results.Content(returnContent, response.Content.Headers.ContentType?.ToString(), Encoding.UTF8)
		};
	}

	#endregion

	#region Methods: Public

	public override int Execute(StartServerOptions options){
		_app.Urls.Add($"http://*:{options.Port}");
		_app.Run();
		return 0;
	}

	#endregion

}