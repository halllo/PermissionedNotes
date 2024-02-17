using Microsoft.AspNetCore.Authentication;
using Permissions;
using PermissionedNotes.Service;
using Serilog;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
builder.Host
	.UseSerilog((ctx, cfg) =>
	{
		cfg.ReadFrom.Configuration(ctx.Configuration);
		cfg.Enrich.WithProperty("EnvironmentName", ctx.HostingEnvironment.EnvironmentName);
	});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddAuth(builder.Configuration);
builder.Services.Configure<PermissionsOptions>(builder.Configuration.GetSection("SpiceDB"));
builder.Services.AddPermissions();
builder.Services.AddDbContext<DB>(o => o.UseNpgsql(builder.Configuration.GetConnectionString("Notes")!).UseSnakeCaseNamingConvention());









var app = builder.Build();

app.UseHttpsRedirection();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
	app.Use(async (http, next) =>
	{
		if (http.Request.Path.StartsWithSegments("/swagger") && (http.User.Identity?.IsAuthenticated) != true)
			await http.ChallengeAsync(Auth.LoginCookie);
		else
			await next();
	});
	app.UseSwagger();
	app.UseSwaggerUI();
}

app.MapControllers();

app.Run();
