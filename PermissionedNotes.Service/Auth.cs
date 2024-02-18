using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace PermissionedNotes.Service
{
	public static class Auth
	{
		public const string LoginCookie = nameof(LoginCookie);
		public static void AddAuth(this IServiceCollection services)
		{
			const string ExternalLoginScheme = nameof(ExternalLoginScheme);
			services.AddAuthentication(LoginCookie)
				.AddCookie(LoginCookie, options =>
				{
					options.Cookie.Name = $"permissionednotes.authn";
					options.Cookie.SameSite = SameSiteMode.Strict;
					options.ForwardChallenge = ExternalLoginScheme;
					options.Events.OnRedirectToAccessDenied = new Func<RedirectContext<CookieAuthenticationOptions>, Task>(context =>
					{
						context.Response.StatusCode = StatusCodes.Status403Forbidden;
						return context.Response.CompleteAsync();
					});
				})
				.AddOpenIdConnect(ExternalLoginScheme, o =>
				{
					o.ClientId = "permissionednotes.web";
					o.ClientSecret = "secret";
					o.Authority = "https://localhost:5001";
					o.ResponseType = OpenIdConnectResponseType.Code;
					o.Scope.Add("openid");
					o.Scope.Add("profile");
					o.Scope.Add("verification");
					o.Scope.Add("notes");
					o.Scope.Add("admin");
					o.SaveTokens = true;
					o.Events.OnRedirectToIdentityProvider = async ctx =>
					{
						if (ctx.ProtocolMessage.RequestType == OpenIdConnectRequestType.Authentication)
						{
							if (ctx.Request.Headers.ContainsKey("X-Requested-With") && ctx.Request.Headers["X-Requested-With"] == "XMLHttpRequest")
							{
								// Request is coming from JavaScript within the page. We return 403.
								ctx.Response.StatusCode = StatusCodes.Status403Forbidden;
								await ctx.Response.CompleteAsync();
							}
							else
							{
								// Request is coming from the browser's frontchannel. We follow redirect to the login page.
							}
						}
					};
					o.Events.OnTicketReceived = async ctx =>
					{
						//Get scope claims from access token
						var accesstoken = ctx.Properties?.GetTokenValue("access_token");
						var token = new JwtSecurityTokenHandler().ReadJwtToken(accesstoken);
						var scopeClaims = token.Claims.Where(c => c.Type == "scope").Select(c => c.Value);

						//Remember scope claims for policy checking
						var cp = new ClaimsIdentity(ExternalLoginScheme);
						cp.AddClaims(scopeClaims.Select(s => new Claim("scope", s)));
						ctx.Principal!.AddIdentity(cp);

						//Remove all tokens to reduce cookie size
						ctx.Properties?.GetTokens().ToList().ForEach(token => ctx.Properties.UpdateTokenValue(token.Name, string.Empty));
					};
				})
				;

			services.AddAuthorization(options =>
			{
				options.AddPolicy("notes", policy =>
				{
					policy.AuthenticationSchemes = new[] { LoginCookie };
					policy.RequireAuthenticatedUser();
					policy.RequireAssertion(ctx => ctx.User.HasClaim("scope", "notes"));
				});
				options.AddPolicy("admin", policy =>
				{
					policy.AuthenticationSchemes = new[] { LoginCookie };
					policy.RequireAuthenticatedUser();
					policy.RequireAssertion(ctx => ctx.User.HasClaim("scope", "admin"));
				});
			});
		}

		public static Guid Id(this ClaimsPrincipal user)
		{
			var id = user.FindFirstValue(ClaimTypes.NameIdentifier);
			return Guid.Parse(id!);
		}
	}
}
