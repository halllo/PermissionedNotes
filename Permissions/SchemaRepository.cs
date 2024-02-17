using com.authzed.api.v1;
using Microsoft.Extensions.Options;

namespace Permissions
{
	public interface ISchemaRepository
	{
		Task WriteSchema(string schema);
		Task<string> ReadSchema();
	}

	internal class SchemaRepository : GrpcRepository, ISchemaRepository
	{
		private readonly SchemaService.SchemaServiceClient schemaServiceClient;

		public SchemaRepository(SchemaService.SchemaServiceClient schemaServiceClient, IOptionsMonitor<PermissionsOptions> options) : base(options)
		{
			this.schemaServiceClient = schemaServiceClient;
		}

		public async Task WriteSchema(string schema)
		{
			await schemaServiceClient.WriteSchemaAsync(new WriteSchemaRequest { Schema = schema }, headers: AuthHeaders());
		}

		public async Task<string> ReadSchema()
		{
			var currentSchema = await schemaServiceClient.ReadSchemaAsync(new ReadSchemaRequest { }, headers: AuthHeaders());
			return currentSchema.SchemaText;
		}
	}
}
