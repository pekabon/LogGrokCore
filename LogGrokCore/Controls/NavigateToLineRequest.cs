using System;

namespace LogGrokCore.Controls
{
    public class NavigateToLineRequest
    {
        public event Action<int>? Navigate;

        public void Raise(int lineNumber)
        {
            Navigate?.Invoke(lineNumber);
        }
    }
}
