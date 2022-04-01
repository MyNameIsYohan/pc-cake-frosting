using Cake.Common.Tools.DotNet;
using Cake.Common.Tools.DotNet.Build;
using Cake.Core.Diagnostics;
using Cake.Frosting;
using Overleaf.Cake.Frosting.Models;

namespace Overleaf.Cake.Frosting.Tasks
{
    [TaskName(Constants.TaskBuild)]
    public class BuildTask : FrostingTask<BuildContext>
    {
        public override void Run(BuildContext context)
        {
            context.Log.Information($"Run build task with build configuration: {context.Config.MsBuildConfiguration}");
            context.DotNetBuild(context.Config.SolutionFile, new DotNetBuildSettings
            {
                Configuration = context.Config.MsBuildConfiguration
            });
        }
    }
}
