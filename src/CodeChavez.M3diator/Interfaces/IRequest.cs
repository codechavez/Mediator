namespace CodeChavez.M3diator.Interfaces;

// Marker interface for a command or query that returns a response
public interface IRequest<TResponse> { }

// Marker interface for a command that returns nothing (void)
public interface IRequest { }

// Command handler interface
public interface IRequestHandler<TRequest> where TRequest : IRequest
{
    Task Handle(TRequest request, CancellationToken cancellationToken);
}

// Query handler interface
public interface IRequestHandler<in TRequest, TResponse> where TRequest : IRequest<TResponse>
{
    Task<TResponse> Handle(TRequest request, CancellationToken cancellationToken);
}
