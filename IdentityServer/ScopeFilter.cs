namespace IdentityServer;

/// Dominick Baier advices against mixing identity (authentication) and permissions (authorization): https://leastprivilege.com/2016/12/16/identity-vs-permissions/
/// I still think it can be a good idea to combine both aspects, because it dramatically simplifies resource providers. 
internal class ScopeFilter
{
	public IEnumerable<T> FilterScopes<T>(IEnumerable<T> scopes, Func<T, string> selector, string subjectId)
	{
		if (subjectId != TestUsers.Alice.SubjectId)
		{
			scopes = scopes.Where(s => !selector(s).Contains("admin"));//only alice can request admin scope
		}

		return scopes;
	}
}
