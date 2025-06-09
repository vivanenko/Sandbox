using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace DropShippingService.Core;

public static class CoreExtensions
{
    public static IServiceCollection AddSupplierAdapters(this IServiceCollection services, 
        params Assembly[]? assemblies)
    {
        if (assemblies.Length == 0)
            assemblies = AppDomain.CurrentDomain.GetAssemblies();
        Register<IInventorySync>(services, assemblies);
        Register<IProductSync>(services, assemblies);
        Register<IShippingProbe>(services, assemblies);
        return services;
    }

    private static void Register<T>(IServiceCollection services, IEnumerable<Assembly> assemblies)
    {
        var contract = typeof(T);
        var attrQuery = assemblies
            .SelectMany(a => a.GetTypes())
            .Where(t => t.IsClass && !t.IsAbstract && t.IsAssignableTo(contract))
            .SelectMany(t => t.GetCustomAttributes<SupplierServiceAttribute>().Select(attr => (Impl: t, attr)));
        foreach (var (impl, attr) in attrQuery)
        {
            services.AddKeyedTransient(impl, attr.Key);
            services.AddKeyedTransient(contract, attr.Key, impl);
        }
    }
}