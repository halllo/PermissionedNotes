using Duende.IdentityServer.Configuration;
using Duende.IdentityServer.Configuration.Configuration;
using Duende.IdentityServer.Configuration.Models;
using Duende.IdentityServer.Configuration.Models.DynamicClientRegistration;
using Duende.IdentityServer.Configuration.RequestProcessing;
using Duende.IdentityServer.Models;
using Duende.IdentityServer.ResponseHandling;
using Duende.IdentityServer.Stores;
using Duende.IdentityServer.Validation;
using System.Collections.Specialized;
using System.Security.Claims;

namespace IdentityServer
{
	public static class McpExtensions
	{
		public static IIdentityServerBuilder AddOAuthDiscoveryEndpoint(this IIdentityServerBuilder builder)
		{
			var discoveryEndpoint = builder.Services
				.Where(s => s.ImplementationInstance is Duende.IdentityServer.Hosting.Endpoint endpoint && endpoint.Name == Duende.IdentityServer.IdentityServerConstants.EndpointNames.Discovery)
				.Select(s => s.ImplementationInstance as Duende.IdentityServer.Hosting.Endpoint)
				.FirstOrDefault();

			if (discoveryEndpoint != null)
			{
				var oauthDiscoveryPath = "/.well-known/oauth-authorization-server";
				builder.Services.AddSingleton(new Duende.IdentityServer.Hosting.Endpoint(discoveryEndpoint.Name + 2, oauthDiscoveryPath, discoveryEndpoint.Handler));
				builder.Services.Configure<IdentityServerOptions>(o => o.Cors.CorsPaths.Add(oauthDiscoveryPath));
			}

			return builder;
		}

		public static IdentityServerConfigurationBuilder AddDynamicClientRegistration(this IdentityServerConfigurationBuilder builder)
		{
			builder.Services.AddTransient<IDynamicClientRegistrationRequestProcessor, CustomClientRegistrationProcessor>();
			builder.Services.AddTransientDecorator<IDiscoveryResponseGenerator, CustomDiscoveryResponseGenerator>();
			builder.Services.Configure<IdentityServerOptions>(o => o.Cors.CorsPaths.Add("/connect/dcr"));
			return builder;
		}

		public static IIdentityServerBuilder ReplaceEmptyScopesWithResourceScopes(this IIdentityServerBuilder builder)
		{
			builder.Services.AddTransientDecorator<IAuthorizeRequestValidator, CustomAuthorizeRequestValidator>();
			return builder;
		}

		private class CustomClientRegistrationProcessor(
			IdentityServerConfigurationOptions options, 
			IClientConfigurationStore dcrStore,
			IResourceStore store,
			IClientStore clientStore) : DynamicClientRegistrationRequestProcessor(options, dcrStore)
		{
			public override async Task<IDynamicClientRegistrationResponse> ProcessAsync(DynamicClientRegistrationContext context)
			{
				if (!context.Client.AllowedScopes.Any())
				{
					var resources = await store.GetAllEnabledResourcesAsync();//todo: only get resources available to current user
					foreach (var scope in resources.ApiResources.SelectMany(api => api.Scopes).Distinct())
					{
						context.Client.AllowedScopes.Add(scope);
					}
				}

				var processed = await base.ProcessAsync(context);
				return processed;
			}

			protected override async Task<IStepResult> AddClientId(DynamicClientRegistrationContext context)
			{
				if (context.Request.Extensions.TryGetValue("client_id", out var clientIdParameter))
				{
					var clientId = clientIdParameter.ToString();
					if (clientId != null)
					{
						var existingClient = clientStore.FindClientByIdAsync(clientId);
						if (existingClient is not null)
						{
							return new DynamicClientRegistrationError(
								"Duplicate client id",
								"Attempt to register a client with a client id that has already been registered"
							);
						}
						else
						{
							context.Client.ClientId = clientId;
							return new SuccessfulStep();
						}
					}
				}
				return await base.AddClientId(context);
			}

			protected override async Task<(Secret, string)> GenerateSecret(DynamicClientRegistrationContext context)
			{
				if (context.Request.Extensions.TryGetValue("client_secret", out var secretParam))
				{
					var plainText = secretParam.ToString();
					ArgumentNullException.ThrowIfNull(plainText);
					var secret = new Secret(plainText.Sha256());

					return (secret, plainText);
				}
				return await base.GenerateSecret(context);
			}
		}

		private class CustomDiscoveryResponseGenerator : IDiscoveryResponseGenerator
		{
			private readonly IDiscoveryResponseGenerator inner;

			public CustomDiscoveryResponseGenerator(Decorator<IDiscoveryResponseGenerator> inner)
			{
				this.inner = inner.Instance;
			}

			public async Task<Dictionary<string, object>> CreateDiscoveryDocumentAsync(string baseUrl, string issuerUri)
			{
				var discoveryDocument = await inner.CreateDiscoveryDocumentAsync(baseUrl, issuerUri);

				var orderedDiscoveryDocuments = new OrderedDictionary<string, object>(discoveryDocument);
				var indexOfFirstNonHttpEntry = discoveryDocument.Index().SkipWhile(e => e.Item.Value?.ToString()?.StartsWith("http") ?? false).FirstOrDefault().Index;
				orderedDiscoveryDocuments.Insert(indexOfFirstNonHttpEntry, "registration_endpoint", baseUrl + "/connect/dcr");
				return orderedDiscoveryDocuments.ToDictionary();
			}

			public Task<IEnumerable<JsonWebKey>> CreateJwkDocumentAsync()
			{
				return inner.CreateJwkDocumentAsync();
			}
		}

		private class CustomAuthorizeRequestValidator : IAuthorizeRequestValidator
		{
			private readonly IAuthorizeRequestValidator inner;
			private readonly IResourceStore store;

			public CustomAuthorizeRequestValidator(Decorator<IAuthorizeRequestValidator> inner, IResourceStore store)
			{
				this.inner = inner.Instance;
				this.store = store;
			}

			public async Task<AuthorizeRequestValidationResult> ValidateAsync(NameValueCollection parameters, ClaimsPrincipal? subject = null, AuthorizeRequestType authorizeRequestType = AuthorizeRequestType.Authorize)
			{
				if (parameters.Get("scope") == null && parameters.Get("resource") != null)
				{
					var resource = parameters.Get("resource");
					var apiResources = await this.store.FindEnabledApiResourcesByNameAsync([resource ?? string.Empty]);
					parameters.Add("scope", string.Join(' ', apiResources.SelectMany(api => api.Scopes).Distinct()));
				}

				var validated = await inner.ValidateAsync(parameters, subject, authorizeRequestType);
				return validated;
			}
		}
	}
}
