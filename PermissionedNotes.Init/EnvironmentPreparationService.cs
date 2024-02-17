using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PermissionedNotes.Service;
using Permissions;

namespace PermissionedNotes.Init
{
	internal class EnvironmentPreparationService : IHostedService
	{
		private readonly ILogger logger;
		private readonly IHostApplicationLifetime appLifetime;
		private readonly IServiceProvider serviceProvider;
		private readonly IConfiguration config;

		public EnvironmentPreparationService(
			ILogger<EnvironmentPreparationService> logger,
			IHostApplicationLifetime appLifetime,
			IServiceProvider serviceProvider,
			IConfiguration config)
		{
			this.logger = logger;
			this.appLifetime = appLifetime;
			this.serviceProvider = serviceProvider;
			this.config = config;
		}

		public Task StartAsync(CancellationToken cancellationToken)
		{
			this.appLifetime.ApplicationStarted.Register(() =>
			{
				Task.Run(async () =>
				{
					try
					{
						await DoIt(cancellationToken);
					}
					catch (Exception ex)
					{
						this.logger.LogError(ex, "Unhandled exception!");
						Environment.ExitCode = 2;
					}
					finally
					{
						this.appLifetime.StopApplication();
					}
				});
			});

			return Task.CompletedTask;
		}

		public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

		private async Task DoIt(CancellationToken cancellationToken)
		{
			this.logger.LogInformation("Starting environmental preparation...");
			using (var serviceScope = serviceProvider.CreateScope())
			{
				{//Apply permissions schema
					var schema = File.ReadAllText("schema.zed");
					if (!string.IsNullOrWhiteSpace(schema))
					{
						this.logger.LogInformation("Migrating permissions schema...");
						var permissions = serviceScope.ServiceProvider.GetService<ISchemaRepository>();
						if (permissions is not null)
						{
							await permissions.WriteSchema(schema);
							var schemaRead = await permissions.ReadSchema();
							this.logger.LogInformation("Updated permissions schema: {Schema}", schemaRead);
						}
						else
						{
							this.logger.LogCritical("Schema repository not found.");
						}
					}
				}
				{//Apply DB migrations
					this.logger.LogInformation("Migrating DB...");
					var context = serviceScope.ServiceProvider.GetRequiredService<DB>();
					await context.Database.MigrateAsync(cancellationToken);
				}
			}
			this.logger.LogInformation("Successfully prepared the environment.");
		}
	}
}
