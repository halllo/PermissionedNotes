using Microsoft.Extensions.DependencyInjection.Extensions;

namespace IdentityServer
{
	public static class DiHelper
	{
		public static void AddTransientDecorator<TService, TImplementation>(this IServiceCollection services)
			where TService : class
			where TImplementation : class, TService
		{
			services.AddDecorator<TService>();
			services.AddTransient<TService, TImplementation>();
		}

		public static void AddScopedDecorator<TService, TImplementation>(this IServiceCollection services)
			where TService : class
			where TImplementation : class, TService
		{
			services.AddDecorator<TService>();
			services.AddScoped<TService, TImplementation>();
		}

		private static void AddDecorator<TService>(this IServiceCollection services)
		{
			var registration = services.LastOrDefault(x => x.ServiceType == typeof(TService));
			if (registration == null)
			{
				throw new InvalidOperationException("Service type: " + typeof(TService).Name + " not registered.");
			}
			if (services.Any(x => x.ServiceType == typeof(Decorator<TService>)))
			{
				throw new InvalidOperationException("Decorator already registered for type: " + typeof(TService).Name + ".");
			}

			services.Remove(registration);

			if (registration.ImplementationInstance != null)
			{
				var type = registration.ImplementationInstance.GetType();
				var innerType = typeof(Decorator<,>).MakeGenericType(typeof(TService), type);
				services.Add(new ServiceDescriptor(typeof(Decorator<TService>), innerType, ServiceLifetime.Transient));
				services.Add(new ServiceDescriptor(type, registration.ImplementationInstance));
			}
			else if (registration.ImplementationFactory != null)
			{
				services.Add(new ServiceDescriptor(typeof(Decorator<TService>), provider =>
				{
					return new DisposableDecorator<TService>((TService)registration.ImplementationFactory(provider));
				}, registration.Lifetime));
			}
			else
			{
				var type = registration.ImplementationType!;
				var innerType = typeof(Decorator<,>).MakeGenericType(typeof(TService), type);
				services.Add(new ServiceDescriptor(typeof(Decorator<TService>), innerType, ServiceLifetime.Transient));
				services.Add(new ServiceDescriptor(type, type, registration.Lifetime));
			}
		}

		public static void ReplaceTransient<TService, TImplementation>(this IServiceCollection services)
		{
			var descriptor = new ServiceDescriptor(
					typeof(TService),
					typeof(TImplementation),
					ServiceLifetime.Transient);
			services.Replace(descriptor);
		}
	}

	public class Decorator<TService>
	{
		public TService Instance { get; set; }

		public Decorator(TService instance)
		{
			Instance = instance;
		}
	}

	public class Decorator<TService, TImpl> : Decorator<TService>
		where TImpl : class, TService
	{
		public Decorator(TImpl instance) : base(instance)
		{
		}
	}

	public class DisposableDecorator<TService> : Decorator<TService>, IDisposable
	{
		public DisposableDecorator(TService instance) : base(instance)
		{
		}

		public void Dispose()
		{
			(Instance as IDisposable)?.Dispose();
		}
	}
}
