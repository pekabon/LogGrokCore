
namespace LogGrokCore
{
    public class ViewSettings
    {
        public enum ViewBigLine
        {
            Prune = 0,
            Break
        }

        public ViewBigLine BigLine { get; set; } = ViewBigLine.Break;
        public int BigLineSize { get; set; } = 9728;

    }
}