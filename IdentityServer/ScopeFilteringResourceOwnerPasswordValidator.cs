using Duende.IdentityServer.Models;
using Duende.IdentityServer.Validation;

namespace IdentityServer;

internal class ScopeFilteringResourceOwnerPasswordValidator : IResourceOwnerPasswordValidator
{
	private readonly ScopeFilter scopeFilter;

	public ScopeFilteringResourceOwnerPasswordValidator(ScopeFilter scopeFilter)
	{
		this.scopeFilter = scopeFilter;
	}

	public Task ValidateAsync(ResourceOwnerPasswordValidationContext context)
	{
		var user = TestUsers.Users.Where(u => u.Username == context.UserName && u.Password == context.Password).FirstOrDefault();
		if (user == null)
		{
			context.Result = new GrantValidationResult(TokenRequestErrors.InvalidGrant, "invalid credential");
		}
		else
		{
			context.Request.ValidatedResources.ParsedScopes = this.scopeFilter.FilterScopes(context.Request.ValidatedResources.ParsedScopes, s => s.ParsedName, user.SubjectId).ToList();
			context.Result = new GrantValidationResult(subject: user.SubjectId, authenticationMethod: "password");
		}
		return Task.CompletedTask;
	}
}