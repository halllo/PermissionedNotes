using Grpc.Core;
using Microsoft.Extensions.Options;

namespace Permissions
{
	internal abstract class GrpcRepository
	{
		private readonly IOptionsMonitor<PermissionsOptions> options;

		public GrpcRepository(IOptionsMonitor<PermissionsOptions> options)
		{
			this.options = options;
		}

		protected Metadata AuthHeaders() => new Metadata { { "Authorization", $"Bearer {this.options.CurrentValue.BearerAuthorization}" } };
	}
}
