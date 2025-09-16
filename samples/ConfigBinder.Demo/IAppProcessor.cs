namespace ConfigBinder.Demo;

public interface IAppProcessor
{
    Task<int> ProcessAsync(AppConfig config, CancellationToken cancellationToken);
}