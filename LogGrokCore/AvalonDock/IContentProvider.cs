namespace LogGrokCore.AvalonDock
{
    public interface IContentProvider
    {
        public object? GetContent(string contentId);
    }
}