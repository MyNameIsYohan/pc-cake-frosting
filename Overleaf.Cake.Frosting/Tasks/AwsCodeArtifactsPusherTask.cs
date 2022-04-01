using Amazon;
using Amazon.CodeArtifact;
using Amazon.CodeArtifact.Model;
using Amazon.Runtime;
using Cake.Common.Build;
using Cake.Common.Diagnostics;
using Cake.Common.IO;
using Cake.Common.Tools.NuGet;
using Cake.Common.Tools.NuGet.Push;
using Cake.Common.Tools.NuGet.Sources;
using Cake.Core.IO;
using Cake.Frosting;
using Overleaf.Cake.Frosting.Models;
using System;
using System.Threading.Tasks;

namespace Overleaf.Cake.Frosting.Tasks
{
    [TaskName(Constants.TaskPushToCodeArtifact)]
    public class AwsCodeArtifactsPusherTask : AsyncFrostingTask<BuildContext>
    {
        public override async Task RunAsync(BuildContext context)
        {
            var branchName = context.Config.GitVersion.BranchName?.ToLower() ?? "";
            context.Information($"{nameof(AwsCodeArtifactsPusherTask)} Branch: {branchName}");

            //only push to CA for releasable branches: development, release, hotfix and master
            //This to prevent pushing package for the non releasable branch such as bugfix/feature
            if (branchName.StartsWith("development")
               || branchName.StartsWith("release")
               || branchName.StartsWith("hotfix")
               || branchName.StartsWith("master"))
            {
                string awsAccessKey = context.Config.AwsCodeArtifactAwsAccessKey;
                var awsSecretKey = context.Config.AwsCodeArtifactAwsSecret;
                var awsRegion = context.Config.AwsRegion;

                try
                {
                    using (var artifactClient = CreateAmazonCodeArtifactClient(awsAccessKey, awsSecretKey, awsRegion, context))
                    {
                        var repoUrlResponse = await artifactClient.GetRepositoryEndpointAsync(new GetRepositoryEndpointRequest
                        {
                            Domain = context.Config.AwsCodeArtifactsDomain,
                            DomainOwner = context.Config.AwsCodeArtifactsDomainOwner,
                            Repository = context.Config.AwsCodeArtifactsRepo,
                            Format = PackageFormat.Nuget
                        });
                        var repoUrl = $"{repoUrlResponse.RepositoryEndpoint}v3/index.json";

                        context.Information($"Repo url:{repoUrl}");

                        var tokenResponse = await artifactClient.GetAuthorizationTokenAsync(new GetAuthorizationTokenRequest
                        {
                            Domain = context.Config.AwsCodeArtifactsDomain,
                            DomainOwner = context.Config.AwsCodeArtifactsDomainOwner,
                            DurationSeconds = 900
                        });
                        PushPackages(context, repoUrl, tokenResponse);

                    }
                }
                catch (Exception ex)
                {
                    context.Error($"Error while pushing to code artifact, Error:{ex.Message}");
                    if ((context.TeamCity().IsRunningOnTeamCity))
                        throw;
                }
            }
            else
            {
                context.Information($"Package is not pushed to code artifact");
            }
        }

        private static void PushPackages(BuildContext context, string repoUrl, GetAuthorizationTokenResponse tokenResponse)
        {
            if (!bool.TryParse(context.Config.AwsCodeArtifactNugetPushSkipDuplicates, out bool skipDuplicates))
                skipDuplicates = true; // set default to true if no argument passed

            var nugetToolPath = new FilePath($"{context.Tools.Resolve("nuget.exe").GetDirectory()}/{"nuget.exe"}");

            string sourceName = context.Config.AwsCodeArtifactsSourceName;
            try
            {
                GracefullyRemoveSource(context, repoUrl, sourceName);

                context.NuGetAddSource(
                    sourceName,
                    repoUrl,
                    new NuGetSourcesSettings
                    {
                        UserName = "aws",
                        Password = tokenResponse.AuthorizationToken,
                        ToolPath = nugetToolPath
                    });

                var packages = context.GetFiles($"{context.Config.ArtifactsFolder}/*.nupkg");

                context.NuGetPush(
                    packages,
                    new NuGetPushSettings
                    {
                        Source = sourceName,
                        SkipDuplicate = skipDuplicates,
                        ToolPath = nugetToolPath
                    });
            }
            finally
            {
                GracefullyRemoveSource(context, repoUrl, sourceName);
            }
            void GracefullyRemoveSource(BuildContext context, string repoUrl, string sourceName)
            {
                try
                {
                    context.NuGetRemoveSource(
                        sourceName,
                        repoUrl,
                        new NuGetSourcesSettings { ToolPath = nugetToolPath });
                }
                catch (Exception)
                {
                    context.Information($"Unable to find any package source(s) matching name: {sourceName}");
                }
            }
        }

        private AmazonCodeArtifactClient CreateAmazonCodeArtifactClient(string awsAccessKey, string awsSecretKey, string awsRegion, BuildContext context)
        {
            // add credentials if available , otherwise it AWS will try to authenticate against the origin machine
            if (!string.IsNullOrWhiteSpace(awsAccessKey) && !string.IsNullOrWhiteSpace(awsSecretKey))
            {
                context.Information("Creating AmazonCodeArtifactClient using access key and secret keys");
                var awsCredentials = new BasicAWSCredentials(awsAccessKey, awsSecretKey);
                return new AmazonCodeArtifactClient(awsCredentials, RegionEndpoint.GetBySystemName(awsRegion));
            }
            else
            {
                context.Information("Creating AmazonCodeArtifactClient using the machine IAM role /profile");
                return new AmazonCodeArtifactClient(RegionEndpoint.GetBySystemName(awsRegion));
            }
        }
    }
}
