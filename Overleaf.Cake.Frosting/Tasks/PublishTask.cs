using Cake.Common.Diagnostics;
using Cake.Common.IO;
using Cake.Common.Tools.DotNet;
using Cake.Common.Tools.DotNet.Publish;
using Cake.Core.IO;
using Cake.Frosting;
using Overleaf.Cake.Frosting.Models;

namespace Overleaf.Cake.Frosting.Tasks
{
    [TaskName(Constants.TaskPublish)]
    public class PublishTask : FrostingTask<BuildContext>
    {
        public override void Run(BuildContext context)
        {
            foreach (var publishProject in context.Config.PublishProjects)
            {
                //always do clean the publish folder 
                context.Information($"Publishing: {publishProject.ProjectFile} to: {context.Config.PublishFolder}");
                context.CleanDirectory(context.Config.PublishFolder);

                context.DotNetPublish(publishProject.ProjectFile,
                new DotNetPublishSettings
                {
                    Configuration = context.Config.MsBuildConfiguration,
                    NoBuild = true,
                    NoRestore = true,
                    OutputDirectory = context.Config.PublishFolder
                });
                if (publishProject.ZipOutput)
                {
                    context.CreateDirectory(context.Config.ArtifactsFolder);
                    context.Zip(context.Config.PublishFolder, new FilePath($"{context.Config.ArtifactsFolder}/{publishProject.ZipFileName}"));

                    if (!string.IsNullOrWhiteSpace(publishProject.ConfigFolder) && !string.IsNullOrWhiteSpace(publishProject.ConfigZipFileName))
                    {
                        if (context.DirectoryExists(publishProject.ConfigFolder))
                        {
                            var zipOutput = $"{context.Config.ArtifactsFolder}/{publishProject.ConfigZipFileName}";
                            context.Information($"Config folder found for: {publishProject.ProjectFile} Zip output: {zipOutput}");
                            context.Zip(publishProject.ConfigFolder, new FilePath(zipOutput));
                        }
                        else
                        {
                            context.Information($"No config folder found for: {publishProject.ProjectFile}");
                        }
                    }
                }
            }

        }
    }
}
