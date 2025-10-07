using CodeChavez.M3diator.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Concurrent;
using System.Linq.Expressions;

namespace CodeChavez.M3diator;

/// <summary>
/// M3diator implementation allow access to Handle and Notification handlers
/// </summary>
public class M3diator : IM3diator
{
    private readonly IServiceProvider _serviceProvider;
    private static readonly ConcurrentDictionary<Type, Func<IServiceProvider, object, CancellationToken, ValueTask>> _voidHandlers = new();
    private static readonly ConcurrentDictionary<Type, Func<IServiceProvider, object, CancellationToken, ValueTask<object>>> _responseHandlers = new();
    private static readonly ConcurrentDictionary<Type, Func<IServiceProvider, object, CancellationToken, Task>> _notificationHandlers = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="M3diator"/> class
    /// </summary>
    /// <param name="sp">Service Provider</param>
    public M3diator(IServiceProvider sp)
    {
        _serviceProvider = sp;
    }

    /// <summary>
    /// Handles requests with Response
    /// </summary>
    /// <typeparam name="TResponse"></typeparam>
    /// <param name="request"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
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
                new[] { handlerType },
                spParam
            );

            var castRequest = Expression.Convert(reqParam, reqType);
            var callHandle = Expression.Call(getHandlerCall, handleMethod, castRequest, ctParam);

            // Convert Task<TResponse> to ValueTask<object>
            var wrapCall = Expression.Call(
                typeof(M3diator),
                nameof(WrapTaskAsValueTask),
                new[] { typeof(TResponse) },
                callHandle
            );

            var lambda = Expression.Lambda<Func<IServiceProvider, object, CancellationToken, ValueTask<object>>>(
                wrapCall,
                spParam, reqParam, ctParam
            );

            return lambda.Compile();
        });

        var result = await handlerFunc(_serviceProvider, request!, ct);
        return (TResponse)result;
    }

    private static async ValueTask<object> WrapTaskAsValueTask<T>(Task<T> task)
    {
        var result = await task.ConfigureAwait(false);
        return result!;
    }

    /// <summary>
    /// Handles requests with no response
    /// </summary>
    /// <typeparam name="TRequest"></typeparam>
    /// <param name="request"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
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
                new[] { handlerType },
                spParam
            );

            var castRequest = Expression.Convert(reqParam, reqType);
            var callHandle = Expression.Call(getHandlerCall, handleMethod, castRequest, ctParam);

            // Convert Task to ValueTask
            var wrapCall = Expression.Call(
                typeof(M3diator),
                nameof(WrapTaskAsValueTaskVoid),
                Type.EmptyTypes,
                callHandle
            );

            var lambda = Expression.Lambda<Func<IServiceProvider, object, CancellationToken, ValueTask>>(
                wrapCall,
                spParam, reqParam, ctParam
            );

            return lambda.Compile();
        });

        await handlerFunc(_serviceProvider, request!, ct);
    }
    
    private static async ValueTask WrapTaskAsValueTaskVoid(Task task)
    {
        await task.ConfigureAwait(false);
    }
    
    /// <summary>
    /// Publishes INotification
    /// </summary>
    /// <typeparam name="TNotification"></typeparam>
    /// <param name="notification"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
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
