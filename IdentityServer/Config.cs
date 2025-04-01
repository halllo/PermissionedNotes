using Duende.IdentityModel;
using Duende.IdentityServer;
using Duende.IdentityServer.Models;

namespace IdentityServer;

public static class Config
{
	public static IEnumerable<IdentityResource> IdentityResources =>
	[
		new IdentityResources.OpenId(),
		new IdentityResources.Profile(),
		new IdentityResource()
		{
			Name = "verification",
			UserClaims = new List<string>
			{
				JwtClaimTypes.Email,
				JwtClaimTypes.EmailVerified
			}
		}
	];

	public static IEnumerable<ApiScope> ApiScopes =>
	[
		new ApiScope(name: "notes", displayName: "Notes"),
		new ApiScope(name: "admin", displayName: "Administration")
	];

	public static IEnumerable<Client> Clients =>
	[
		new Client
		{
			ClientId = "permissionednotes.web",
			ClientSecrets = { new Secret("secret".Sha256()) },
			AllowedGrantTypes = [..GrantTypes.Code, ..GrantTypes.ResourceOwnerPassword],
			RedirectUris = { "https://localhost:7262/signin-oidc" },
			PostLogoutRedirectUris = { "https://localhost:7262/signout-callback-oidc" },
			AllowedScopes =
			{
				IdentityServerConstants.StandardScopes.OpenId,
				IdentityServerConstants.StandardScopes.Profile,
				"verification",
				"notes",
				"admin"
			}
		},
		new Client
		{
			ClientId = "bot1",
			ClientSecrets = { new Secret("secret".Sha256()) },
			AllowedGrantTypes = [..GrantTypes.Code],
			RedirectUris = { "https://europe.token.botframework.com/.auth/web/redirect"	},
			PostLogoutRedirectUris = { },
			AllowedScopes =
			{
				IdentityServerConstants.StandardScopes.OpenId,
				IdentityServerConstants.StandardScopes.Profile,
				"verification",
				"notes",
				"admin"
			},
			RequirePkce = false,
			AllowOfflineAccess = true,
		}
	];
}