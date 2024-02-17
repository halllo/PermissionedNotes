using Duende.IdentityServer.Extensions;
using Duende.IdentityServer.Validation;

namespace IdentityServer;

internal class ScopeFilteringScopeParser : IScopeParser
{
	private readonly DefaultScopeParser defaultScopeParser;
	private readonly ScopeFilter scopeFilter;
	private readonly IHttpContextAccessor httpContextAccessor;

	public ScopeFilteringScopeParser(ILogger<DefaultScopeParser> logger, ScopeFilter scopeFilter, IHttpContextAccessor httpContextAccessor)
	{
		this.defaultScopeParser = new DefaultScopeParser(logger);
		this.scopeFilter = scopeFilter;
		this.httpContextAccessor = httpContextAccessor;
	}

	public ParsedScopesResult ParseScopeValues(IEnumerable<string> scopeValues)
	{
		var user = this.httpContextAccessor.HttpContext?.User;
		if (user?.IsAuthenticated() == true)//only works with authenticated users like auth code. does not work with resource owner password grant
		{
			scopeValues = this.scopeFilter.FilterScopes(scopeValues, s => s, user.GetSubjectId());
		}

		return defaultScopeParser.ParseScopeValues(scopeValues);
	}
}
