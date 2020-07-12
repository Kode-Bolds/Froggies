using System;
using Unity.Properties;

namespace Unity.Build.Common
{
    public sealed class PlayerScriptingDefines : IBuildComponent
    {
        [CreateProperty]
        public string[] Defines { get; set; } = Array.Empty<string>();
    }
}
