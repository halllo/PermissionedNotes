// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.

using Duende.IdentityModel;
using Duende.IdentityServer;
using Duende.IdentityServer.Test;
using System.Security.Claims;
using System.Text.Json;

namespace IdentityServer;

public static class TestUsers
{
	private static readonly string Address = JsonSerializer.Serialize(new
	{
		street_address = "One Hacker Way",
		locality = "Heidelberg",
		postal_code = "69118",
		country = "Germany"
	});

	public static readonly TestUser Alice = new TestUser
	{
		SubjectId = Guid.Empty.ToString()[..^1] + "1",
		Username = "alice",
		Password = "alice",
		Claims =
		{
			new Claim(JwtClaimTypes.Name, "Alice Smith"),
			new Claim(JwtClaimTypes.GivenName, "Alice"),
			new Claim(JwtClaimTypes.FamilyName, "Smith"),
			new Claim(JwtClaimTypes.Email, "AliceSmith@email.com"),
			new Claim(JwtClaimTypes.EmailVerified, "false", ClaimValueTypes.Boolean),
			new Claim(JwtClaimTypes.WebSite, "http://alice.com"),
			new Claim(JwtClaimTypes.Address, Address, IdentityServerConstants.ClaimValueTypes.Json)
		}
	};
	public static readonly TestUser Bob = new TestUser
	{
		SubjectId = Guid.Empty.ToString()[..^1] + "2",
		Username = "bob",
		Password = "bob",
		Claims =
		{
			new Claim(JwtClaimTypes.Name, "Bob Smith"),
			new Claim(JwtClaimTypes.GivenName, "Bob"),
			new Claim(JwtClaimTypes.FamilyName, "Smith"),
			new Claim(JwtClaimTypes.Email, "BobSmith@email.com"),
			new Claim(JwtClaimTypes.EmailVerified, "true", ClaimValueTypes.Boolean),
			new Claim(JwtClaimTypes.WebSite, "http://bob.com"),
			new Claim(JwtClaimTypes.Address, Address, IdentityServerConstants.ClaimValueTypes.Json)
		}
	};

	public static List<TestUser> Users => [Alice, Bob];
}