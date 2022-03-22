namespace LogGrokCore.AvalonDockExtensions;

public interface IContentProvider
{
    public object? GetContent(string contentId);
}