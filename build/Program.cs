using Cake.Common;
using Cake.Common.Build;
using Cake.Common.Diagnostics;
using Cake.Common.IO;
using Cake.Common.Tools.DotNet;
using Cake.Common.Tools.DotNetCore.Build;
using Cake.Common.Tools.DotNetCore.MSBuild;
using Cake.Common.Tools.DotNetCore.NuGet.Push;
using Cake.Common.Tools.DotNetCore.Pack;
using Cake.Common.Tools.GitVersion;
using Cake.Core;
using Cake.Core.IO;
using Cake.Frosting;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;

namespace Build
{
    public static class Program
    {
        public static int Main(string[] args)
        {
            return new CakeHost()
                .UseWorkingDirectory("..")
                .InstallTools()
                .UseContext<BuildContext>()
                .Run(args);
        }
    }
    public static class ToolsInstaller
    {
        public static CakeHost InstallTools(this CakeHost host)
        {
            host.SetToolPath($"./caketools");
            host.InstallTool(new Uri("nuget:?package=JetBrains.dotCover.CommandLineTools&version=2021.1.3"));
            host.InstallTool(new Uri("nuget:?package=MSBuild.SonarQube.Runner.Tool&version=4.8.0"));
            return host;
        }
    }
    public class BuildContext : FrostingContext
    {
        public string MsBuildConfiguration { get; set; }
        public string ApplicationVersion { get; set; }
        public GitVersion GitVersion { get; set; }
        public Options Options { get; private set; }

        public BuildContext(ICakeContext context)
            : base(context)
        {
            MsBuildConfiguration = context.Argument("configuration", "Release");
            var optionJson = File.ReadAllText("build/options.json");
            Options = JsonConvert.DeserializeObject<Options>(optionJson);
        }
    }

    [ExcludeFromCodeCoverage]
    [TaskName("Clean")]
    public sealed class CleanTask : FrostingTask<BuildContext>
    {
        public override void Run(BuildContext context)
        {
            var dirs = Directory.GetDirectories(@"./Overleaf.Cake.Frosting", "bin", SearchOption.AllDirectories).ToList();
            foreach (var dir in dirs)
            {
                context.CleanDirectory($"{dir}/{context.MsBuildConfiguration}");
            }
        }
    }

    [ExcludeFromCodeCoverage]
    [TaskName("Version")]
    public sealed class VersionTask : FrostingTask<BuildContext>
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
                    context.Information("Setting teamicty version using NuGetVersionV2");
                    context.TeamCity().SetBuildNumber(context.ApplicationVersion);
                }
            }
            void CalculateVersion()
            {
                var version = context.GitVersion();
                Console.WriteLine($"Version : {version.NuGetVersionV2}");
                context.ApplicationVersion = version.NuGetVersionV2;
                context.GitVersion = version;
            }
        }
    }

    [ExcludeFromCodeCoverage]
    [TaskName("Sonar-Init")]
    public sealed class SonarInitTask : SonarInit
    { }

    [TaskName("Build")]
    public sealed class BuildTask : FrostingTask<BuildContext>
    {
        public override void Run(BuildContext context)
        {
            context.DotNetBuild(context.Options.SolutionFile, new DotNetCoreBuildSettings
            {
                Configuration = context.MsBuildConfiguration
            });
        }
    }

    [ExcludeFromCodeCoverage]
    [TaskName("Sonar-End")]
    public sealed class SonarEndTask : SonarEnd
    { }

    [ExcludeFromCodeCoverage]
    [TaskName("Package")]
    public sealed class PackageTask : FrostingTask<BuildContext>
    {
        public override void Run(BuildContext context)
        {
            var msBuildSettings = new DotNetCoreMSBuildSettings();
            msBuildSettings.Properties.Add("PackageVersion", new List<string> { context.ApplicationVersion });

            context.DotNetPack(context.Options.SolutionFile, new DotNetCorePackSettings
            {
                OutputDirectory = context.Options.ArtifactsFolder,
                MSBuildSettings = msBuildSettings,
                NoBuild = true,
                NoRestore = true,
                Configuration = context.MsBuildConfiguration
            });
        }

    }

    [ExcludeFromCodeCoverage]
    [TaskName("package-push")]
    public sealed class PackagePushTask : FrostingTask<BuildContext>
    {
        public override void Run(BuildContext context)
        {
            var packges = context.GetFiles(context.Options.ArtifactsFolder + "/*.nupkg");

            if (context.TeamCity().IsRunningOnTeamCity && context.GitVersion != null)
            {
                var feedUrl = "";
                var feedApiKey = context.TeamCity().Environment.Build.ConfigProperties["MyGetApiKey"];

                if (context.GitVersion.BranchName == "master" || context.GitVersion.BranchName.StartsWith("release"))
                {
                    context.Information("Publishing to 'parknow-dotnet'...");
                    feedUrl = context.TeamCity().Environment.Build.ConfigProperties["MyGetFeedPushUrl"];
                }
                else if (context.GitVersion.BranchName == "development" || context.GitVersion.BranchName.StartsWith("feature"))
                {
                    context.Information("Publishing to 'parknow-dotnet-preview'...");
                    feedUrl = context.TeamCity().Environment.Build.ConfigProperties["MyGetPreviewFeedPushUrl"];
                }
                else
                {
                    context.Information("Unsupported branch found, aborting NuGet publish..");
                    return;
                }
                foreach (var packge in packges)
                {
                    try
                    {
                        context.DotNetNuGetPush(packge.FullPath, new DotNetCoreNuGetPushSettings
                        {
                            Source = feedUrl,
                            ApiKey = feedApiKey
                        });
                    }
                    catch (Exception ex)
                    {
                        context.Warning(ex.Message);
                    }

                }
            }
        }
    }

    [IsDependentOn(typeof(CleanTask))]
    [IsDependentOn(typeof(VersionTask))]
    [IsDependentOn(typeof(SonarInitTask))]
    [IsDependentOn(typeof(BuildTask))]
    [IsDependentOn(typeof(SonarEndTask))]
    [IsDependentOn(typeof(PackageTask))]
    [IsDependentOn(typeof(PackagePushTask))]
    public sealed class Default : FrostingTask
    {
    }
}
