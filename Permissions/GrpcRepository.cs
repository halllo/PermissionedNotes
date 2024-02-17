using Grpc.Core;
using Microsoft.Extensions.Configuration;

namespace Permissions
{
	internal abstract class GrpcRepository
	{
		private readonly IConfiguration config;

		public GrpcRepository(IConfiguration config)
		{
			this.config = config;
		}

		protected Metadata AuthHeaders() => new Metadata { { "Authorization", $"Bearer {this.config["SpiceDB:BearerAuthorization"]}" } };
	}
}
