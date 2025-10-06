namespace CodeChavez.M3diator.Interfaces;

public interface IHandl3
{
    Task<TResponse> Handle<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default);
    Task Handle<TRequest>(TRequest request, CancellationToken cancellationToken = default) where TRequest : IRequest;
}
