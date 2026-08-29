using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using PlainMediator.Abstractions;

namespace PlainMediator;

/// <summary>
/// Registration helpers for PlainMediator.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="IMediator"/> and discovers every <see cref="IRequestHandler{TRequest, TResponse}"/>
    /// and <see cref="INotificationHandler{TNotification}"/> in the given scope. Pipeline behaviors are not
    /// discovered: register them yourself, in the order you want them to run.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="args">
    /// Scope of the handler scan. Accepts either:
    /// nothing (scans every assembly currently loaded in the AppDomain);
    /// one or more <see cref="Assembly"/> instances (scans exactly those);
    /// or one or more <see cref="string"/> prefixes (scans loaded assemblies whose name starts with a prefix — the cheapest option).
    /// </param>
    /// <exception cref="ArgumentException"><paramref name="args"/> mixes assemblies and strings, or contains another type.</exception>
    public static IServiceCollection AddMediator(this IServiceCollection services, params object[] args)
    {
        ArgumentNullException.ThrowIfNull(services);

        var assemblies = ResolveAssemblies(args);

        services.TryAddScoped<IMediator, Mediator>();

        RegisterHandlers(services, assemblies, typeof(INotificationHandler<>));
        RegisterHandlers(services, assemblies, typeof(IRequestHandler<,>));

        return services;
    }

    private static Assembly[] ResolveAssemblies(object[] args)
    {
        if (args is null || args.Length == 0)
        {
            return LoadedAssemblies().ToArray();
        }

        if (Array.TrueForAll(args, a => a is Assembly))
        {
            return [.. args.Cast<Assembly>().Distinct()];
        }

        if (Array.TrueForAll(args, a => a is string))
        {
            var prefixes = args.Cast<string>().ToArray();

            return [.. LoadedAssemblies()
                .Where(a => Array.Exists(prefixes, p => a.FullName!.StartsWith(p, StringComparison.Ordinal)))];
        }

        throw new ArgumentException(
            "Invalid arguments for AddMediator(). Use no arguments, one or more Assembly instances, or one or more assembly-name prefixes.",
            nameof(args));
    }

    private static IEnumerable<Assembly> LoadedAssemblies() =>
        AppDomain.CurrentDomain
            .GetAssemblies()
            .Where(a => !a.IsDynamic && !string.IsNullOrWhiteSpace(a.FullName))
            .Distinct();

    private static void RegisterHandlers(IServiceCollection services, Assembly[] assemblies, Type handlerInterface)
    {
        foreach (var type in assemblies.SelectMany(GetLoadableTypes))
        {
            if (!type.IsClass || type.IsAbstract || type.IsGenericTypeDefinition)
            {
                continue;
            }

            var registered = false;

            foreach (var contract in type.GetInterfaces())
            {
                if (!contract.IsGenericType || contract.GetGenericTypeDefinition() != handlerInterface)
                {
                    continue;
                }

                // Scoped mantém os handlers compatíveis com dependências scoped (DbContext, HttpClient, ...).
                services.TryAddEnumerable(ServiceDescriptor.Scoped(contract, type));
                registered = true;
            }

            // Expõe também o tipo concreto, para os casos em que ele é resolvido diretamente.
            if (registered)
            {
                services.TryAddScoped(type);
            }
        }
    }

    private static IEnumerable<Type> GetLoadableTypes(Assembly assembly)
    {
        // Uma única dependência que não carrega em um assembly varrido não pode derrubar o startup.
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            return ex.Types.Where(t => t is not null)!;
        }
    }
}
