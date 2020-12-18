using System;
using System.Security.Principal;
using System.Text;
using NLog;
using NLog.LayoutRenderers;

namespace LogGrokCore.Diagnostics
{
    [LayoutRenderer("IsElevated")]
    public class ElevationLayoutRenderer : LayoutRenderer
    {
        protected override void Append(StringBuilder builder, LogEventInfo _)
        {
            builder.Append((_isElevated.Value)  ? "-Elevated" : string.Empty);
        }

        private static bool IsElevated()
        {
            var principal = new WindowsPrincipal(WindowsIdentity.GetCurrent());
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }

        private readonly Lazy<bool> _isElevated = new(IsElevated);
    }
}
