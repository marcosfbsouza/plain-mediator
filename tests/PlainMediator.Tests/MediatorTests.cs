using Microsoft.Extensions.DependencyInjection;
using PlainMediator.Abstractions;

namespace PlainMediator.Tests;

public class MediatorTests
{
    private static ServiceProvider BuildProvider(Action<IServiceCollection>? configure = null)
    {
        var services = new ServiceCollection();
        services.AddSingleton<Trace>();
        services.AddMediator(typeof(Ping).Assembly);
        configure?.Invoke(services);
        return services.BuildServiceProvider(validateScopes: true);
    }

    [Fact]
    public async Task Send_encaminha_a_request_para_o_handler()
    {
        using var provider = BuildProvider();
        using var scope = provider.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        var response = await mediator.Send(new Ping("hello"));

        Assert.Equal("pong:hello", response);
    }

    [Fact]
    public async Task Send_lanca_excecao_quando_nao_ha_handler_registrado()
    {
        using var provider = BuildProvider();
        using var scope = provider.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => mediator.Send(new Unhandled()));

        Assert.Contains(nameof(Unhandled), exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Send_repassa_o_cancellation_token_para_o_handler()
    {
        using var provider = BuildProvider();
        using var scope = provider.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        using var cts = new CancellationTokenSource();

        var received = await mediator.Send(new CancellablePing(), cts.Token);

        Assert.Equal(cts.Token, received);
    }

    [Fact]
    public async Task Behaviors_executam_na_ordem_de_registro()
    {
        using var provider = BuildProvider(services =>
        {
            services.AddScoped(typeof(IPipelineBehavior<,>), typeof(FirstBehavior<,>));
            services.AddScoped(typeof(IPipelineBehavior<,>), typeof(SecondBehavior<,>));
        });

        using var scope = provider.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        await mediator.Send(new Ping("hello"));

        var trace = provider.GetRequiredService<Trace>();
        Assert.Equal(
            ["first:before", "second:before", "second:after", "first:after"],
            trace.Steps);
    }

    [Fact]
    public async Task Um_behavior_pode_interromper_a_cadeia_antes_do_handler()
    {
        using var provider = BuildProvider(services =>
            services.AddScoped<IPipelineBehavior<Ping, string>, ShortCircuitBehavior>());

        using var scope = provider.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        var response = await mediator.Send(new Ping("hello"));

        Assert.Equal("short-circuited", response);
        Assert.Equal(["short-circuit"], provider.GetRequiredService<Trace>().Steps);
    }

    [Fact]
    public async Task Publish_alcanca_todos_os_handlers_na_ordem_de_registro()
    {
        using var provider = BuildProvider();
        using var scope = provider.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        await mediator.Publish(new Notified("go"));

        Assert.Equal(["first:go", "second:go"], provider.GetRequiredService<Trace>().Steps);
    }

    [Fact]
    public async Task Publish_resolve_os_handlers_pelo_tipo_em_tempo_de_execucao()
    {
        using var provider = BuildProvider();
        using var scope = provider.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        INotification notification = new Notified("go");
        await mediator.Publish(notification);

        Assert.Equal(["first:go", "second:go"], provider.GetRequiredService<Trace>().Steps);
    }

    [Fact]
    public async Task Publish_nao_faz_nada_quando_ninguem_observa_a_notificacao()
    {
        using var provider = BuildProvider();
        using var scope = provider.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        await mediator.Publish(new Unobserved());

        Assert.Empty(provider.GetRequiredService<Trace>().Steps);
    }

    [Fact]
    public async Task Send_rejeita_uma_request_nula()
    {
        using var provider = BuildProvider();
        using var scope = provider.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        await Assert.ThrowsAsync<ArgumentNullException>(() => mediator.Send<string>(null!));
    }
}
