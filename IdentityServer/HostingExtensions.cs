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

        builder.Services.AddRazorPages();

		builder.Services.AddIdentityServer()
			.AddInMemoryIdentityResources(Config.IdentityResources)
			.AddInMemoryApiScopes(Config.ApiScopes)
			.AddInMemoryClients(Config.Clients)
			.AddTestUsers(TestUsers.Users)
			.AddScopeParser<ScopeFilteringScopeParser>()
			.AddResourceOwnerValidator<ScopeFilteringResourceOwnerPasswordValidator>()
			.AddProfileService<CustomProfileService>()
			;

		builder.Services.AddAuthentication();
		builder.Services.AddTransient<ScopeFilter>();

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

		return app;
	}
}
