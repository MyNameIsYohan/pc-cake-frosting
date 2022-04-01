using Cake.Common;
using Cake.Common.Diagnostics;
using Cake.Core.IO;
using Cake.Frosting;
using Cake.Sonar;

namespace Build
{
    public  class SonarInit : FrostingTask<BuildContext>
    {

        public override void Run(BuildContext context)
        {
            var sonarUrl = context.Argument<string>("sonarServerUrl", context.EnvironmentVariable<string>("sonarServerUrl", null));
            var sonarLogin = context.Argument<string>("sonarServerLogin", context.EnvironmentVariable<string>("sonarServerLogin", null));
            var pullRequestNumber = context.Argument<string>("pullRequestNumber", null);
            var pullRequestSourceBranch = context.Argument<string>("pullRequestSourceBranch", null);
            var pullRequestTargetBranch = context.Argument<string>("pullRequestTargetBranch", null);
            if (string.IsNullOrWhiteSpace(sonarUrl))
            {
                context.Information("Skipping Sonar integration since url is not specified");
                return;
            }

            var dotCoverReportsPath = new FilePath(context.Options.CoverageReport).FullPath;
            var isPullRequest = context.GitVersion.BranchName.ToLower().Contains("pull-request") && !context.GitVersion.BranchName.ToLower().Contains("merge");
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
                        Url = sonarUrl,
                        Login = sonarLogin,
                        Verbose = false,
                        Key = context.Options.SonarQubeProjectKey,
                        Name = context.Options.ApplicationName,
                        Version = context.ApplicationVersion,
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
                    Url = sonarUrl,
                    Login = sonarLogin,
                    Verbose = false,
                    Key = context.Options.SonarQubeProjectKey,
                    Name = context.Options.ApplicationName,
                    Version = context.ApplicationVersion,
                    DotCoverReportsPath = dotCoverReportsPath,
                    Branch = context.GitVersion.BranchName
                });
            }
        }
    }

    public class SonarEnd : FrostingTask<BuildContext>
    {

        public override void Run(BuildContext context)
        {
            var sonarUrl = context.Argument<string>("sonarServerUrl", context.EnvironmentVariable<string>("sonarServerUrl", null));
            var sonarLogin = context.Argument<string>("sonarServerLogin", context.EnvironmentVariable<string>("sonarServerLogin", null));
            if (!CheckSonarUp(sonarUrl))
            {
                context.Information("Skipping Sonar integration since server is not reachable");
                return;
            }

            context.SonarEnd(new SonarEndSettings { Login = sonarLogin });
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
