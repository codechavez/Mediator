using CodeChavez.M3diator.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Concurrent;
using System.Linq.Expressions;

namespace CodeChavez.M3diator;

public class M3diator : IM3diator
{
    private readonly IServiceProvider _serviceProvider;
    private static readonly ConcurrentDictionary<Type, Func<IServiceProvider, object, CancellationToken, ValueTask>> _voidHandlers = new();
    private static readonly ConcurrentDictionary<Type, Func<IServiceProvider, object, CancellationToken, ValueTask<object>>> _responseHandlers = new();
    private static readonly ConcurrentDictionary<Type, Func<IServiceProvider, object, CancellationToken, Task>> _notificationHandlers = new();

    public M3diator(IServiceProvider sp)
    {
        _serviceProvider = sp;
    }

    // Handle commands/queries with responses
    public async Task<TResponse> Handle<TResponse>(IRequest<TResponse> request, CancellationToken ct = default)
    {
        var handlerFunc = _responseHandlers.GetOrAdd(request.GetType(), static reqType =>
        {
            var spParam = Expression.Parameter(typeof(IServiceProvider), "sp");
            var reqParam = Expression.Parameter(typeof(object), "request");
            var ctParam = Expression.Parameter(typeof(CancellationToken), "ct");

            var handlerType = typeof(IRequestHandler<,>).MakeGenericType(reqType, typeof(TResponse));
            var handleMethod = handlerType.GetMethod("Handle")!;

            var getHandlerCall = Expression.Call(
                typeof(ServiceProviderServiceExtensions),
                nameof(ServiceProviderServiceExtensions.GetRequiredService),
                [handlerType],
                spParam
            );

            var castRequest = Expression.Convert(reqParam, reqType);
            var callHandle = Expression.Call(getHandlerCall, handleMethod, castRequest, ctParam);

            var castResult = Expression.Convert(callHandle, typeof(object));
            var lambda = Expression.Lambda<Func<IServiceProvider, object, CancellationToken, ValueTask<object>>>(
                Expression.Convert(castResult, typeof(ValueTask<object>)),
                spParam, reqParam, ctParam
            );

            return lambda.Compile();
        });

        var result = await handlerFunc(_serviceProvider, request!, ct);
        return (TResponse)result;
    }

    // Handle commands with no response
    public async Task Handle<TRequest>(TRequest request, CancellationToken ct = default) where TRequest : IRequest
    {
        var handlerFunc = _voidHandlers.GetOrAdd(request.GetType(), static reqType =>
        {
            var spParam = Expression.Parameter(typeof(IServiceProvider), "sp");
            var reqParam = Expression.Parameter(typeof(object), "request");
            var ctParam = Expression.Parameter(typeof(CancellationToken), "ct");

            var handlerType = typeof(IRequestHandler<>).MakeGenericType(reqType);
            var handleMethod = handlerType.GetMethod("Handle")!;

            var getHandlerCall = Expression.Call(
                typeof(ServiceProviderServiceExtensions),
                nameof(ServiceProviderServiceExtensions.GetRequiredService),
                [handlerType],
                spParam
            );

            var castRequest = Expression.Convert(reqParam, reqType);
            var callHandle = Expression.Call(getHandlerCall, handleMethod, castRequest, ctParam);

            var lambda = Expression.Lambda<Func<IServiceProvider, object, CancellationToken, ValueTask>>(
                Expression.Convert(callHandle, typeof(ValueTask)),
                spParam, reqParam, ctParam
            );

            return lambda.Compile();
        });

        await handlerFunc(_serviceProvider, request!, ct);
    }

    public async Task Publish<TNotification>(TNotification notification, CancellationToken ct = default)
        where TNotification : INotification
    {
        var handlers = _serviceProvider.GetServices<INotificationHandler<TNotification>>().ToList();
        if (handlers.Count == 0)
            return;

        // Run in parallel for throughput (handlers are independent)
        var tasks = new Task[handlers.Count];
        for (int i = 0; i < handlers.Count; i++)
            tasks[i] = handlers[i].Handle(notification, ct);

        await Task.WhenAll(tasks);
    }
}
