using Microsoft.Extensions.DependencyInjection;
using System;

namespace Kraken.Caching
{
	public static class ServiceCollectionExtensions
	{
		public static IServiceCollection AddCaching(this IServiceCollection services)
		{
			services.AddSingleton(typeof(MemoryCache));
			services.AddScoped(typeof(ScopedMemoryCache));
			return services;
		}

		public static IServiceCollection AddCached<TService, TImplementation>(this IServiceCollection services)
			where TService : class
			where TImplementation : class, TService
		{
			// TODO Add lifetime
			Type proxy = new CachedGenerator<TService, TImplementation>().Type;
			services.AddTransient(typeof(TService), proxy);
			services.AddTransient(typeof(NonCached<TImplementation>));
			services.AddTransient(typeof(TImplementation));
			return services;
		}

		public static IServiceCollection AddCached(this IServiceCollection services, Type svcType, Type impType)
		{
			// TODO Add lifetime
			Type proxy = new CachedGenerator(svcType, impType).Type;
			services.AddTransient(svcType, proxy);
			services.AddTransient(typeof(NonCached<>).MakeGenericType(new[] { impType }));
			services.AddTransient(impType);
			return services;
		}

		// TODO Add more Adds to register type from parameters
	}
}
