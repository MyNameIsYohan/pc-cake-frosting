using Cake.Common;
using Cake.Common.Diagnostics;
using Cake.Common.IO;
using Cake.Core;
using Cake.Core.IO;
using Cake.Frosting;
using Overleaf.Cake.Frosting.Models;
using System;

namespace Overleaf.Cake.Frosting.Tasks
{
    [TaskName(Constants.TaskParameterStoreUploader)]
    public class ParameterStoreUploadTask : FrostingTask<BuildContext>
    {
        public override void Run(BuildContext context)
        {
            FilePath parameterUploader = context.Tools.Resolve("ParameterStoreUploader.dll");
            var settingsFiles = context.GetFiles(context.Config.ArtifactsFolder + "/*jsonSettings.zip");

            if (!context.FileExists(parameterUploader))
            {
                throw new Exception("ParameterStoreUploader not found.");
            }

            var applicationVersion = context.Config.ApplicationVersion;
            if (string.IsNullOrWhiteSpace(applicationVersion))
            {
                applicationVersion = context.Environment.GetEnvironmentVariable(Constants.ApplicationVersion);
            }

            var kmsAlias = context.Environment.GetEnvironmentVariable(Constants.ApplicationUploaderKmsAlias);
            var uploaderPrivateKey = context.Environment.GetEnvironmentVariable(Constants.ApplicationUploaderPrivateKeyString);

            var applicationEnvironment = context.Environment.GetEnvironmentVariable(Constants.ApplicationEnvironment);
            if (string.IsNullOrWhiteSpace(applicationEnvironment))
            {
                throw new Exception("ParameterStoreUploader | Cannot find environment variable 'Application__Environment'");
            }

            var region = context.Environment.GetEnvironmentVariable(Constants.AwsDefaultRegion);
            if (string.IsNullOrWhiteSpace(region))
            {
                region = context.Environment.GetEnvironmentVariable(Constants.AwsRegion);
                if (string.IsNullOrWhiteSpace(region))
                {
                    throw new Exception("ParameterStoreUploader | Cannot find environment variable 'AWS_DEFAULT_REGION'");
                }
            }

            var awsProfile = context.Environment.GetEnvironmentVariable(Constants.AwsProfile);
            var awsAccessKey = context.Environment.GetEnvironmentVariable(Constants.AwsAccessKeyId);
            var awsSecretKey = context.Environment.GetEnvironmentVariable(Constants.AwsSecretAccessKey);
            if (string.IsNullOrWhiteSpace(awsProfile) && string.IsNullOrEmpty(awsAccessKey)) //either using profile or access secret key
            {
                throw new Exception("ParameterStoreUploader | Cannot find either AWS Profile or AWS Access/Secret key");
            }

            if (string.IsNullOrWhiteSpace(kmsAlias))
            {
                context.Information("Cannot find Uploader KMS alias from env var. Set default value");
                kmsAlias = "alias/dev-bic-security-kms-general_purpose_key";
            }

            if (string.IsNullOrWhiteSpace(uploaderPrivateKey))
            {
                context.Information("Cannot find Uploader private key from env var. Set default value");
                uploaderPrivateKey = "no_value";
            }

            foreach (var file in settingsFiles)
            {
                context.Information($"Found settings zip file: {file.FullPath}");

                var extractDirectory = file.GetDirectory().Combine(new DirectoryPath(file.GetFilenameWithoutExtension().ToString()));
                context.Information($"Extracting to {extractDirectory}");

                context.Unzip(file, extractDirectory);

                var pab = new ProcessArgumentBuilder()
                     .Append(parameterUploader.FullPath)
                    .AppendSwitch("-f", extractDirectory.FullPath)
                    .AppendSwitch("-e", applicationEnvironment)
                    .AppendSwitch("-l", extractDirectory.FullPath)
                    .AppendSwitch("-a", context.Config.ApplicationPlatfrom)
                    .AppendSwitch("-n", context.Config.ApplicationSystem)
                    .AppendSwitch("--sub-system-name", context.Config.ApplicationSubsystem)
                    .AppendSwitch("--system-version", applicationVersion)
                    .AppendSwitch("-r", region)
                    .AppendSwitch("-i", kmsAlias)
                    .AppendSwitch("-p", uploaderPrivateKey);

                if (!string.IsNullOrWhiteSpace(awsAccessKey) && !string.IsNullOrWhiteSpace(awsSecretKey))
                {
                    pab.AppendSwitch("-k", awsAccessKey).AppendSwitch("-s", awsSecretKey);
                }
                else
                {
                    pab.AppendSwitch("-z", awsProfile);
                }

                context.Information($"Executing: ParameterStoreUploader.dll {pab.Render()}");
                var exitCode = context.StartProcess("dotnet", new ProcessSettings { Arguments = pab, RedirectStandardOutput = true });

                if (exitCode != 0)
                {
                    context.Error($"Failed to upload parameters: {file.FullPath} Code: {exitCode}");
                    throw new ArgumentException("Failed to upload parameters");
                }
                else
                {
                    context.Information($"Successfully uploaded parameters to SSM: /{applicationEnvironment}/{context.Config.ApplicationPlatfrom}/{context.Config.ApplicationSystem}/{context.Config.ApplicationSubsystem}");
                }
            }
        }
    }
}
