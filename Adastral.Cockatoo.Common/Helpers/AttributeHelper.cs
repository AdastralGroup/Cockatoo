using System.Diagnostics;
using System.Reflection;
using Adastral.Cockatoo.Common;
using Microsoft.Extensions.DependencyInjection;

namespace Adastral.Cockatoo.Common.Helpers;

public static class AttributeHelper
{
    public static void InjectControllerAttributes(Assembly assembly, IServiceCollection services)
    {
        var classes = GetTypesWithAttribute<CockatooDependencyAttribute>(assembly);
        var data = new List<(CockatooDependencyAttribute, Type)>();
        foreach (var x in classes ?? [])
        {
            var attr = x.GetCustomAttribute<CockatooDependencyAttribute>();
            if (attr != null)
            {
                data.Add((attr, x));
            }
        }
        foreach (var item in data.OrderBy(v => v.Item1.Priority).Select(v => v.Item2))
        {
            var descriptor = new ServiceDescriptor(item, item, ServiceLifetime.Singleton);
            if (services.Contains(descriptor))
                continue;
            services.AddSingleton(item);
            Trace.WriteLine($"Injected {item}");
        }
    }

    public static IEnumerable<Type> GetTypesWithAttribute<T>(Assembly assembly)
    {
        foreach(Type type in assembly.GetTypes()) {
            if (type.GetCustomAttributes(typeof(T), true).Length > 0 && type.IsAssignableTo(typeof(BaseService))) {
                yield return type;
            }
        }
    }
}