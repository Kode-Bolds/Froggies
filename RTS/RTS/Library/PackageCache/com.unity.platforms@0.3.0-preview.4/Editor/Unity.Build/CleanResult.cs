using System;

namespace Unity.Build
{
    /// <summary>
    /// Container for results happening when cleaning a build.
    /// </summary>
    public sealed class CleanResult : ResultBase
    {
        /// <summary>
        /// Get a clean result representing a success.
        /// </summary>
        /// <param name="pipeline">The build pipeline.</param>
        /// <param name="config">The build configuration.</param>
        /// <returns>A new clean result instance.</returns>
        public static CleanResult Success(BuildPipelineBase pipeline, BuildConfiguration config) => new CleanResult
        {
            Succeeded = true,
            BuildPipeline = pipeline,
            BuildConfiguration = config,
        };

        /// <summary>
        /// Get a clean result representing a failure.
        /// </summary>
        /// <param name="pipeline">The build pipeline.</param>
        /// <param name="config">The build configuration.</param>
        /// <param name="reason">The reason of the failure.</param>
        /// <returns>A new clean result instance.</returns>
        public static CleanResult Failure(BuildPipelineBase pipeline, BuildConfiguration config, string reason) => new CleanResult
        {
            Succeeded = false,
            BuildPipeline = pipeline,
            BuildConfiguration = config,
            Message = reason
        };

        /// <summary>
        /// Get a clean result representing a failure.
        /// </summary>
        /// <param name="pipeline">The build pipeline.</param>
        /// <param name="config">The build configuration.</param>
        /// <param name="exception">The exception that was thrown.</param>
        /// <returns>A new clean result instance.</returns>
        public static CleanResult Failure(BuildPipelineBase pipeline, BuildConfiguration config, Exception exception) => new CleanResult
        {
            Succeeded = false,
            BuildPipeline = pipeline,
            BuildConfiguration = config,
            Exception = exception
        };

        public override string ToString() => $"Clean {base.ToString()}";

        public CleanResult() { }
    }
}
