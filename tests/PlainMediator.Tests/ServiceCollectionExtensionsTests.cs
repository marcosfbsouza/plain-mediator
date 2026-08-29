using Microsoft.Extensions.DependencyInjection;
using PlainMediator.Abstractions;

namespace PlainMediator.Tests;

public class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddMediator_registra_handlers_encontrados_por_assembly()
    {
        var services = new ServiceCollection().AddMediator(typeof(Ping).Assembly);

        Assert.Contains(services, d => d.ServiceType == typeof(IRequestHandler<Ping, string>));
        Assert.Contains(services, d => d.ServiceType == typeof(INotificationHandler<Notified>));
        Assert.Contains(services, d => d.ServiceType == typeof(IMediator));
    }

    [Fact]
    public void AddMediator_registra_handlers_encontrados_por_prefixo_do_nome_do_assembly()
    {
        var services = new ServiceCollection().AddMediator("PlainMediator.Tests");

        Assert.Contains(services, d => d.ServiceType == typeof(IRequestHandler<Ping, string>));
    }

    [Fact]
    public void AddMediator_registra_tambem_o_tipo_concreto_do_handler()
    {
        var services = new ServiceCollection().AddMediator(typeof(Ping).Assembly);

        Assert.Contains(services, d => d.ServiceType == typeof(PingHandler));
    }

    [Fact]
    public void AddMediator_registra_os_handlers_como_scoped()
    {
        var services = new ServiceCollection().AddMediator(typeof(Ping).Assembly);

        Assert.All(
            services.Where(d => d.ServiceType == typeof(IRequestHandler<Ping, string>)),
            d => Assert.Equal(ServiceLifetime.Scoped, d.Lifetime));
    }

    [Fact]
    public void AddMediator_e_idempotente()
    {
        var services = new ServiceCollection()
            .AddMediator(typeof(Ping).Assembly)
            .AddMediator(typeof(Ping).Assembly);

        Assert.Single(services, d => d.ServiceType == typeof(IMediator));
        Assert.Equal(2, services.Count(d => d.ServiceType == typeof(INotificationHandler<Notified>)));
    }

    [Fact]
    public void AddMediator_rejeita_argumentos_que_nao_sao_assemblies_nem_prefixos()
    {
        var services = new ServiceCollection();

        Assert.Throws<ArgumentException>(() => services.AddMediator(42));
        Assert.Throws<ArgumentException>(() => services.AddMediator(typeof(Ping).Assembly, "PlainMediator.Tests"));
    }
}
