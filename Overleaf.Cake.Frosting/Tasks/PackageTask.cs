using Cake.Common.Tools.NuGet;
using Cake.Common.Tools.NuGet.Pack;
using Cake.Frosting;
using Overleaf.Cake.Frosting.Models;

namespace Overleaf.Cake.Frosting.Tasks
{
    [TaskName(Constants.TaskPackage)]
    public class PackageTask : FrostingTask<BuildContext>
    {
        public override void Run(BuildContext context)
        {
            foreach (var nuspec in context.Config.PackagingNuspec)
            {
                context.NuGetPack(nuspec, new NuGetPackSettings
                {
                    Version = context.Config.ApplicationVersion,
                    OutputDirectory = context.Config.ArtifactsFolder,
                });
            }
        }
    }
}
