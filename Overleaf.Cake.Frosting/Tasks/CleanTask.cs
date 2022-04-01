using Cake.Common.IO;
using Cake.Core.Diagnostics;
using Cake.Frosting;
using Overleaf.Cake.Frosting.Models;
using System.IO;

namespace Overleaf.Cake.Frosting.Tasks
{
    [TaskName(Constants.TaskClean)]
    public class CleanTask : FrostingTask<BuildContext>
    {
        public override void Run(BuildContext context)
        {
            context.Log.Information("Run clean task");

            string[] dirs = Directory.GetDirectories(@"./src", "bin", SearchOption.AllDirectories);
            foreach (var dir in dirs)
            {
                context.CleanDirectory($"{dir}/{context.Config.MsBuildConfiguration}");
            }

            //mainly used for local deployment
            if (context.DirectoryExists(context.Config.ArtifactsFolder))
            {
                context.Log.Information("Clean artifact folder");
                context.CleanDirectory(context.Config.ArtifactsFolder);
            }
        }
    }
}
