using Duende.IdentityServer.Configuration;
using Serilog;

namespace IdentityServer;

internal static class HostingExtensions
{
	public static WebApplication ConfigureServices(this WebApplicationBuilder builder)
	{
		builder.Services.AddLogging(loggingBuilder =>
		{
			loggingBuilder.ClearProviders();
			loggingBuilder.AddAzureWebAppDiagnostics();/*requires serilogs writeToProviders: true, until we find a good AzureWebAppDiagnostics sink*/
		});

		builder.Services.AddIdentityServer(o => { })
			.AddInMemoryIdentityResources(Config.IdentityResources)
			.AddInMemoryApiScopes(Config.ApiScopes)
			.AddInMemoryClients(Config.Clients)
			.AddTestUsers(TestUsers.Users)
			.AddScopeParser<ScopeFilteringScopeParser>()
			.AddResourceOwnerValidator<ScopeFilteringResourceOwnerPasswordValidator>()
			.AddProfileService<CustomProfileService>()
			.AddOAuthDiscoveryEndpoint()
			.IngoreMissingScopesDuringAuthorize()
			;

		builder.Services.AddIdentityServerConfiguration(o => { })
			.AddDynamicClientRegistration()
			.AddInMemoryClientConfigurationStore()
			;

		builder.Services.AddAuthentication();
		builder.Services.AddTransient<ScopeFilter>();

		builder.Services.AddRazorPages();

		return builder.Build();
	}


	public static WebApplication ConfigurePipeline(this WebApplication app)
	{
		app.UseSerilogRequestLogging();

		if (app.Environment.IsDevelopment())
		{
			app.UseDeveloperExceptionPage();
		}

		app.UseStaticFiles();
		app.UseRouting();

		app.UseIdentityServer();
		app.UseAuthorization();
		app.MapRazorPages().RequireAuthorization();
		app.MapDynamicClientRegistration().AllowAnonymous();//todo: require auth!

		return app;
	}
}
