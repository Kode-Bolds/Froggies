using System;
using UnityEditor;
using Unity.Build.Common;

namespace Unity.Build.Classic.Private
{
    class ClassicBuildCustomizer : ClassicBuildPipelineCustomizer
    {
        public override Type[] UsedComponents { get; } =
        {
            typeof(ClassicBuildProfile),
            typeof(AutoRunPlayer),
            typeof(EnableHeadlessMode),
            typeof(IncludeTestAssemblies),
            typeof(InstallInBuildFolder),
            typeof(PlayerConnectionSettings),
            typeof(ScriptingDebuggerSettings),
            typeof(PlayerScriptingDefines)
        };

        public override BuildOptions ProvideBuildOptions()
        {
            var options = BuildOptions.None;

            // Build options from build type
            if (Context.TryGetComponent<ClassicBuildProfile>(out var profile))
            {
                switch (profile.Configuration)
                {
                    case BuildType.Debug:
                        options |= BuildOptions.AllowDebugging | BuildOptions.Development;
                        break;
                    case BuildType.Develop:
                        options |= BuildOptions.Development;
                        break;
                }
            }

            // Build options from components
            if (Context.HasComponent<AutoRunPlayer>())
                options |= BuildOptions.AutoRunPlayer;
            if (Context.HasComponent<EnableHeadlessMode>())
                options |= BuildOptions.EnableHeadlessMode;
            if (Context.HasComponent<IncludeTestAssemblies>())
                options |= BuildOptions.IncludeTestAssemblies;
            if (Context.HasComponent<InstallInBuildFolder>())
                options |= BuildOptions.InstallInBuildFolder;
            if (Context.TryGetComponent<PlayerConnectionSettings>(out PlayerConnectionSettings value))
            {
                if (value.Mode == PlayerConnectionInitiateMode.Connect)
                    options |= BuildOptions.ConnectToHost;
                if (value.WaitForConnection)
                    options |= BuildOptions.WaitForPlayerConnection;
            }
            if (Context.HasComponent<ScriptingDebuggerSettings>())
                options |= BuildOptions.AllowDebugging;
            return options;
        }

        public override string[] ProvidePlayerScriptingDefines()
        {
            return Context.GetComponentOrDefault<PlayerScriptingDefines>().Defines;
        }
    }
}
