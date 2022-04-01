using Cake.Common;
using Cake.Common.Build;
using Cake.Common.Diagnostics;
using Cake.Core;
using Cake.Core.Diagnostics;
using Cake.Frosting;
using Newtonsoft.Json;
using Overleaf.Cake.Frosting.Models;
using System;
using System.IO;

namespace Overleaf.Cake.Frosting
{
    public static class Tools
    {
        public static CakeHost InstallTools(this CakeHost host)
        {
            var myGetApiKey = Environment.GetEnvironmentVariable("MyGetApiKey");
            var parameterUploader = "https://www.myget.org/F/parknow-dotnet/auth/" + myGetApiKey + "/api/v2?package=ParameterStoreUploader&version=3.0.0";

            host.SetToolPath($"./caketools");
            host.InstallTool(new Uri("nuget:?package=JetBrains.dotCover.CommandLineTools&version=2020.3.3"));
            host.InstallTool(new Uri("nuget:?package=MSBuild.SonarQube.Runner.Tool&version=4.8.0"));
            host.InstallTool(new Uri("nuget:?package=NuGet.CommandLine&version=5.8.1"));
            host.InstallTool(new Uri($"nuget:{parameterUploader}"));
            return host;
        }

        public static ConfigurationModel GetConfigurationModel(this ICakeContext context)
        {
            context.Log.Information($"Initialize configuration model... Working directory: {context.Environment.WorkingDirectory}");

            //init configuration model from options.json
            var metadataJson = File.ReadAllText($"{context.Environment.WorkingDirectory}/build/options.json");
            var config = JsonConvert.DeserializeObject<ConfigurationModel>(metadataJson);

            //because sonarqube variable name is different in the TC
            if (context.TeamCity().IsRunningOnTeamCity)
            {
                var tcVars = context.TeamCity().Environment.Build.ConfigProperties;
                Environment.SetEnvironmentVariable(Constants.SonarServerUrl, tcVars["SonarQube.ServerUrl"]);
                Environment.SetEnvironmentVariable(Constants.SonarServerLogin, tcVars["SonarQube.Login"]);
                Environment.SetEnvironmentVariable(Constants.PullRequestNumber, tcVars["PullRequestNumber"]);
                Environment.SetEnvironmentVariable(Constants.PullRequestSourceBranch, tcVars["PullRequestSourceBranch"]);
                Environment.SetEnvironmentVariable(Constants.PullRequestTargetBranch, tcVars["PullRequestTargetBranch"]);
            }

            //get the rest of values from TC parameters
            config.MsBuildConfiguration = context.GetVariableValue("configuration", "Release");
            config.DotnetRuntime = context.GetVariableValue("dotnetRuntime", "");
            config.Framework = context.GetVariableValue("framework", "");
            config.ApplicationEnvironment = context.GetVariableValue("applicationEnvironment", context.GetVariableValue(Constants.ApplicationEnvironment));

            config.GitRepoUrl = context.GetVariableValue("gitrepourl", "");
            config.GitUsername = context.GetVariableValue(Constants.GitGenericUserName, "");
            config.GitPassword = context.GetVariableValue(Constants.GitGenericUserPassword, "");
            config.GitRepoRelativePath = context.GetVariableValue("gitreporelativepath", "../");

            config.SonarServerUrl = context.GetVariableValue(Constants.SonarServerUrl);
            config.SonarServerLogin = context.GetVariableValue(Constants.SonarServerLogin);
            config.PullRequestNumber = context.GetVariableValue(Constants.PullRequestNumber);
            config.PullRequestSourceBranch = context.GetVariableValue(Constants.PullRequestSourceBranch);
            config.PullRequestTargetBranch = context.GetVariableValue(Constants.PullRequestTargetBranch);
            config.SonarProjectName = context.GetVariableValue(Constants.SonarProjectName);

            config.AwsProfile = context.GetVariableValue(Constants.AwsProfile);
            config.AwsAccountId = context.GetVariableValue(Constants.AwsAccountId);

            if (config.AwsCodeArtifactsRepo.Equals("bloxx", StringComparison.InvariantCultureIgnoreCase))
            {
                config.AwsCodeArtifactAwsAccessKey = context.GetVariableValue(Constants.CodeArtifactAwsAccessKeyBloxx);
                config.AwsCodeArtifactAwsSecret = context.GetVariableValue(Constants.CodeArtifactAwsSecretBloxx);
                config.AwsCodeArtifactsSourceName = "BloxxAWSCodeArtifacts";
            }
            else
            {
                config.AwsCodeArtifactAwsAccessKey = context.GetVariableValue(Constants.CodeArtifactAwsAccessKeyPhonixx);
                config.AwsCodeArtifactAwsSecret = context.GetVariableValue(Constants.CodeArtifactAwsSecretPhonixx);
                config.AwsCodeArtifactsSourceName = "PhonixxAWSCodeArtifacts";
            }

            config.AwsCodeArtifactNugetPushSkipDuplicates = context.GetVariableValue(Constants.CodeArtifactNugetPushSkipDuplicates);

            config.ImageName = $"{config.ApplicationEnvironment}-{config.ApplicationPlatfrom}-{config.ApplicationSystem}-{config.ApplicationSubsystem}-{config.ApplicationName}";
            config.AwsEcrPath = $"{config.AwsAccountId}.dkr.ecr.{config.AwsRegion}.amazonaws.com";
            return config;
        }

        /// <summary>
        /// Will try to get value from the TC var, if not available then get it from argument, if not available then get it from environment variable
        /// </summary>
        /// <param name="context"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        private static string GetVariableValue(this ICakeContext context, string key, string defaultValue = null)
        {
            var tc = context.TeamCity();
            if (tc.IsRunningOnTeamCity && tc.Environment.Build.ConfigProperties.TryGetValue(key, out string parameterValue))
            {
                context.Information($"Got value from TeamCity: {key}");
                return parameterValue;
            }
            var val = context.Argument(key, context.EnvironmentVariable<string>(key, null));
            return string.IsNullOrWhiteSpace(val) && !string.IsNullOrWhiteSpace(defaultValue)
                ? defaultValue
                : val;
        }

    }
}
