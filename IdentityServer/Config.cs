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

	public static IEnumerable<ApiResource> ApiResources =>
	[
		new ApiResource()
		{
			Name = "http://localhost:5253/",
			Scopes = [ "notes", "admin" ]
		}
	];

	public static IEnumerable<ApiScope> ApiScopes =>
	[
		new ApiScope(name: "notes", displayName: "Notes"),
		new ApiScope(name: "admin", displayName: "Administration")
	];

	public static ICollection<Client> Clients => clients;
	private static readonly List<Client> clients =
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
			RedirectUris = { "https://europe.token.botframework.com/.auth/web/redirect" },
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
		},
		new Client
		{
			ClientId = "mcp_server",
			ClientSecrets = { new Secret("secret".Sha256()) },
			AllowedGrantTypes = [..GrantTypes.Code],
			RedirectUris =
			{
				"https://localhost:7148/signin-oidc",
				"http://localhost:5209/signin-oidc",
				"https://localhost:7296/signin-oidc",
				"http://localhost:5253/signin-oidc",
				"https://localhost:7208/signin-oidc",
				"https://localhost:7054/signin-oidc"
			},
			PostLogoutRedirectUris = { },
			AllowedScopes =
			{
				IdentityServerConstants.StandardScopes.OpenId,
				IdentityServerConstants.StandardScopes.Profile,
				"verification",
				"notes",
				"admin"
			},
			RequirePkce = true,
			AllowOfflineAccess = true,
			AllowedCorsOrigins =
			{
				"http://localhost:6274", // MCP inspector
			}
		}
	];
}