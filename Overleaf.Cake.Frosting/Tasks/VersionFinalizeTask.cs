using Cake.Common.Build;
using Cake.Common.Diagnostics;
using Cake.Frosting;
using Cake.Git;
using Overleaf.Cake.Frosting.Models;
using System;

namespace Overleaf.Cake.Frosting.Tasks
{
    [TaskName(Constants.TaskVersionFinalize)]
    public class VersionFinalizeTask : FrostingTask<BuildContext>
    {
        public override void Run(BuildContext context)
        {
            try
            {
                if (context.TeamCity().IsRunningOnTeamCity && context.Config.GitVersion != null
                    && (context.Config.GitVersion.BranchName.StartsWith("release/") || context.Config.GitVersion.BranchName.StartsWith("hotfix/")))
                {
                    var solutionFolder = "./";
                    context.GitTag(solutionFolder, context.Config.ApplicationVersion);
                    context.Information($"Tag {context.Config.ApplicationVersion} created");
                    context.GitPushRef(solutionFolder, context.Config.GitUsername, context.Config.GitPassword, "origin", context.Config.ApplicationVersion);
                    context.Information($"Tag {context.Config.ApplicationVersion} is pushed");
                }
            }
            catch (Exception ex)
            {
                context.Warning($"Error Finalizing GitVersion, Exception:{ex.Message}");
                if (!ex.Message.Contains("tag already exists"))
                    throw;
            }
        }
    }

    [TaskName(Constants.TaskGitTagMaster)]
    public class GitTagMasterTask : FrostingTask<BuildContext>
    {
        public override void Run(BuildContext context)
        {
            try
            {
                if (context.TeamCity().IsRunningOnTeamCity && context.Config.GitVersion != null && (context.Config.GitVersion.BranchName.Equals("master")))
                {
                    var solutionFolder = "./";
                    context.GitTag(solutionFolder, context.Config.ApplicationVersion);
                    context.Information($"Tag {context.Config.ApplicationVersion} created");
                    context.GitPushRef(solutionFolder, context.Config.GitUsername, context.Config.GitPassword, "origin", context.Config.ApplicationVersion);
                    context.Information($"Tag {context.Config.ApplicationVersion} is pushed");
                }
            }
            catch (Exception ex)
            {
                context.Warning($"Error Finalizing GitVersion, Exception:{ex.Message}");
                if (!ex.Message.Contains("tag already exists"))
                    throw;
            }
        }
    }

}
