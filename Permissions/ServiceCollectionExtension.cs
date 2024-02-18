using com.authzed.api.v1;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Permissions
{
	public static class ServiceCollectionExtension
	{
		public static void AddPermissions(this IServiceCollection services)
		{
			services.AddGrpcClient<SchemaService.SchemaServiceClient>((p, o) =>
			{
				var options = p.GetRequiredService<IOptions<PermissionsOptions>>();
				o.Address = new Uri(options.Value.Endpoint);
			});
			services.AddGrpcClient<PermissionsService.PermissionsServiceClient>((p, o) =>
			{
				var options = p.GetRequiredService<IOptions<PermissionsOptions>>();
				o.Address = new Uri(options.Value.Endpoint);
			});
			services.AddGrpcClient<ExperimentalService.ExperimentalServiceClient>((p, o) =>
			{
				var options = p.GetRequiredService<IOptions<PermissionsOptions>>();
				o.Address = new Uri(options.Value.Endpoint);
			});

			services.AddScoped<ISchemaRepository, SchemaRepository>();
			services.AddScoped<IPermissionsRepository, PermissionsRepository>();
		}
	}
}
