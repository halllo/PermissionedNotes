using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PermissionedNotes.Init;
using PermissionedNotes.Service;
using Permissions;
using Serilog;
using System.Reflection;

var builder = Host.CreateDefaultBuilder(args)
	.UseContentRoot(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!)
	.UseSerilog((ctx, cfg) =>
	{
		cfg.ReadFrom.Configuration(ctx.Configuration);
		cfg.Enrich.WithProperty("EnvironmentName", ctx.HostingEnvironment.EnvironmentName);
	})
	.ConfigureServices((context, services) =>
	{
		services.Configure<PermissionsOptions>(context.Configuration.GetSection("SpiceDB"));
		services.AddPermissions();
		services.AddDbContext<DB>(dbco => dbco
			.UseNpgsql(context.Configuration.GetConnectionString("Notes")!, npgo => npgo.MigrationsAssembly(typeof(DB).Assembly.FullName))
			.UseSnakeCaseNamingConvention());
		services.AddHostedService<EnvironmentPreparationService>();
	});

var app = builder.Build();
app.Run();
