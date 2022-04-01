using Cake.Common.Build;
using Cake.Common.Diagnostics;
using Cake.Common.Tools.GitVersion;
using Cake.Frosting;
using Overleaf.Cake.Frosting.Models;
using System;

namespace Overleaf.Cake.Frosting.Tasks
{
    [TaskName(Constants.TaskGitVersion)]
    public class GitVersionTask : FrostingTask<BuildContext>
    {
        public override void Run(BuildContext context)
        {
            try
            {
                CalculateVersion();
            }
            catch (Exception ex)
            {
                context.Warning($"Error Calculating GitVersion, Retry is in progress, Exception:{ex.Message}");
                CalculateVersion();
            }
            finally
            {
                if (context.TeamCity().IsRunningOnTeamCity)
                {
                    // Set the build number in teamcity.
                    context.Information("Setting teamcity version using NuGetVersionV2");
                    context.TeamCity().SetBuildNumber(context.Config.ApplicationVersion);
                }
            }
            void CalculateVersion()
            {
                var version = context.GitVersion(new GitVersionSettings
                {
                    Verbosity = GitVersionVerbosity.Normal,
                    NoFetch = false
                });
                Console.WriteLine($"Version : {version.NuGetVersionV2}");
                context.Config.ApplicationVersion = version.NuGetVersionV2;
                context.Config.GitVersion = version;
            }
        }
    }
}
