using Unity.Build;
using Unity.Properties;

namespace Unity.Build.Common
{
    public sealed class ScriptingDebuggerSettings : IBuildComponent
    {
        [CreateProperty]
        public bool WaitForManagedDebugger { get; set; } = false;
    }
}
