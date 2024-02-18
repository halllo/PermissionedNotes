// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.


using Duende.IdentityServer;
using Duende.IdentityServer.Models;
using IdentityModel;

namespace IdentityServer;

public static class Config
{
	public static IEnumerable<IdentityResource> IdentityResources =>
		new IdentityResource[]
		{
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
		};

	public static IEnumerable<ApiScope> ApiScopes =>
		new ApiScope[]
		{
			new ApiScope(name: "notes", displayName: "Notes"),
			new ApiScope(name: "admin", displayName: "Administration")
		};

	public static IEnumerable<Client> Clients =>
		new Client[]
		{
			new Client
			{
				ClientId = "permissionednotes.web",
				ClientSecrets = { new Secret("secret".Sha256()) },
				AllowedGrantTypes = GrantTypes.Code,
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
			}
		};
}