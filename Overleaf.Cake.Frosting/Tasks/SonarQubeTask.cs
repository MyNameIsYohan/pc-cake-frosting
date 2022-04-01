using Cake.Common.Diagnostics;
using Cake.Core.IO;
using Cake.Frosting;
using Cake.Sonar;
using Overleaf.Cake.Frosting.Models;

namespace Overleaf.Cake.Frosting.Tasks
{
    [TaskName(Constants.TaskSonarInit)]
    public class SonarInitTask : FrostingTask<BuildContext>
    {
        public override void Run(BuildContext context)
        {
            var dotCoverReportsPath = new FilePath(context.Config.CoverageReport).FullPath;
            var pullRequestNumber = context.Config.PullRequestNumber;
            var pullRequestSourceBranch = context.Config.PullRequestSourceBranch;
            var pullRequestTargetBranch = context.Config.PullRequestTargetBranch;
            var projectName = string.IsNullOrWhiteSpace(context.Config.SonarProjectName)
                ? context.Config.ApplicationName
                : context.Config.SonarProjectName;

            //check if its local deployment
            if (!string.IsNullOrWhiteSpace(context.Config.SonarQubeUrlForLocalDeployment) && !string.IsNullOrWhiteSpace(context.Config.SonarQubeLoginForLocalDeployment))
            {
                context.Information("Start SonarQube for local deployment");
                context.SonarBegin(new SonarBeginSettings
                {
                    Url = context.Config.SonarQubeUrlForLocalDeployment,
                    Login = context.Config.SonarQubeLoginForLocalDeployment,
                    Verbose = true,
                    Key = context.Config.SonarQubeProjectKey,
                    Name = projectName,
                    Version = context.Config.ApplicationVersion,
                    DotCoverReportsPath = dotCoverReportsPath,
                    Branch = context.Config.GitVersion.BranchName
                });
                return;
            }

            if (string.IsNullOrWhiteSpace(context.Config.SonarServerUrl))
            {
                context.Information("Skipping Sonar integration since url is not specified");
                return;
            }

            var isPullRequest = context.Config.GitVersion.BranchName.ToLower().Contains("pull-request") && !context.Config.GitVersion.BranchName.ToLower().Contains("merge");
            context.Information($"is a PR: {isPullRequest}");

            if (isPullRequest)
            {
                context.Information("PR Number: " + pullRequestNumber);
                context.Information("PR source branch: " + pullRequestSourceBranch);
                context.Information("PR target branch: " + pullRequestTargetBranch);
                if (int.TryParse(pullRequestNumber, out int prNumber))
                {
                    context.SonarBegin(new SonarBeginSettings
                    {
                        Url = context.Config.SonarServerUrl,
                        Login = context.Config.SonarServerLogin,
                        Verbose = false,
                        Key = context.Config.SonarQubeProjectKey,
                        Name = projectName,
                        Version = context.Config.ApplicationVersion,
                        DotCoverReportsPath = dotCoverReportsPath,
                        PullRequestBranch = pullRequestSourceBranch,
                        PullRequestBase = pullRequestTargetBranch,
                        PullRequestKey = prNumber
                    });
                }
                else
                {
                    context.Warning($"Skipping pull request analysis , PR number : {pullRequestNumber} couldn't be parsed to an integer");
                }
            }
            else
            {
                context.SonarBegin(new SonarBeginSettings
                {
                    Url = context.Config.SonarServerUrl,
                    Login = context.Config.SonarServerLogin,
                    Verbose = false,
                    Key = context.Config.SonarQubeProjectKey,
                    Name = projectName,
                    Version = context.Config.ApplicationVersion,
                    DotCoverReportsPath = dotCoverReportsPath,
                    Branch = context.Config.GitVersion.BranchName
                });
            }
        }
    }

    [TaskName(Constants.TaskSonarEnd)]
    public class SonarEndTask : FrostingTask<BuildContext>
    {
        public override void Run(BuildContext context)
        {
            if (!CheckSonarUp(context.Config.SonarServerUrl))
            {
                context.Information("Skipping Sonar integration since server is not reachable");
                return;
            }

            if (!string.IsNullOrWhiteSpace(context.Config.SonarQubeLoginForLocalDeployment))
            {
                context.SonarEnd(new SonarEndSettings { Login = context.Config.SonarQubeLoginForLocalDeployment });
            }

            context.SonarEnd(new SonarEndSettings { Login = context.Config.SonarServerLogin });
        }

        private bool CheckSonarUp(string url)
        {
            try
            {
                var version = new SonarServer().GetVersion(url).Result;
                return true;
            }
            catch
            {
                return false;
            }
        }
    }

}
