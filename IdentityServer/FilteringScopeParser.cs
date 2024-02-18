using Duende.IdentityServer.Extensions;
using Duende.IdentityServer.Validation;

namespace IdentityServer
{
	public class FilteringScopeParser : IScopeParser
	{
		private readonly DefaultScopeParser defaultScopeParser;
		private readonly IHttpContextAccessor httpContextAccessor;

		public FilteringScopeParser(ILogger<DefaultScopeParser> logger, IHttpContextAccessor httpContextAccessor)
		{
			this.defaultScopeParser = new DefaultScopeParser(logger);
			this.httpContextAccessor = httpContextAccessor;
		}

		public ParsedScopesResult ParseScopeValues(IEnumerable<string> scopeValues)
		{
			var user = this.httpContextAccessor.HttpContext?.User;
			if (user?.IsAuthenticated() == true)
			{
				if (user.GetSubjectId() != TestUsers.Alice.SubjectId)
				{
					scopeValues = scopeValues.Where(s => !s.Contains("admin"));//only alice can request admin scope
				}
				else
				{
					scopeValues = scopeValues.ToList();
				}
			}

			return defaultScopeParser.ParseScopeValues(scopeValues);
		}
	}
}
