namespace ConfigBinder.Reflection;

public interface IAppProcessor
{
    Task<int> ProcessAsync(AppConfig config, CancellationToken cancellationToken);
}