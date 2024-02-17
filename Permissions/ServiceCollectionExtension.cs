using com.authzed.api.v1;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Permissions
{
	public static class ServiceCollectionExtension
	{
		public static void AddPermissions(this IServiceCollection services, IConfiguration config)
		{
			string? spiceDbEndpoint = config["SpiceDB:Endpoint"];
			if (!string.IsNullOrWhiteSpace(spiceDbEndpoint))
			{
				services.AddGrpcClient<SchemaService.SchemaServiceClient>(o =>
				{
					o.Address = new Uri(spiceDbEndpoint);
				});
				services.AddGrpcClient<PermissionsService.PermissionsServiceClient>(o =>
				{
					o.Address = new Uri(spiceDbEndpoint);
				});
				services.AddGrpcClient<ExperimentalService.ExperimentalServiceClient>(o =>
				{
					o.Address = new Uri(spiceDbEndpoint);
				});

				services.AddScoped<ISchemaRepository, SchemaRepository>();
				services.AddScoped<IPermissionsRepository, PermissionsRepository>();
			}
		}
	}
}
