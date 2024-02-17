using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Permissions;
using PermissionedNotes.Init;
using Serilog;
using System.Reflection;
using PermissionedNotes.Service;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

var builder = Host.CreateDefaultBuilder(args)
	.UseContentRoot(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!)
	.UseSerilog((ctx, cfg) =>
	{
		cfg.ReadFrom.Configuration(ctx.Configuration);
		cfg.Enrich.WithProperty("EnvironmentName", ctx.HostingEnvironment.EnvironmentName);
	})
	.ConfigureServices((context, services) =>
	{
		services.AddPermissions(context.Configuration);
		services.AddDbContext<NotesDbContext>(dbco => dbco
			.UseNpgsql(context.Configuration.GetConnectionString("Notes")!, npgo => npgo.MigrationsAssembly(typeof(NotesDbContext).Assembly.FullName))
			.UseSnakeCaseNamingConvention());
		services.AddHostedService<EnvironmentPreparationService>();
	});

var app = builder.Build();
app.Run();
