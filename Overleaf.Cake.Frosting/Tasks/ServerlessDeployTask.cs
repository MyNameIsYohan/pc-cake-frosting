using Cake.Common;
using Cake.Common.Build;
using Cake.Common.Diagnostics;
using Cake.Common.IO;
using Cake.Core;
using Cake.Core.IO;
using Cake.Frosting;
using Overleaf.Cake.Frosting.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Overleaf.Cake.Frosting.Tasks
{
    [TaskName(Constants.TaskServerlessDeploy)]
    public class ServerlessDeployTask : FrostingTask<BuildContext>
    {
        private string GetParametersOverrides(BuildContext context, string prefix = "Application__")
        {
            var keyPairs = context.EnvironmentVariables()
                .Where(item => item.Key.StartsWith(prefix))
                .Select(item => string.Format(
                    "{0}={1}",
                    item.Key.Replace(prefix, ""),
                    item.Value)
                );

            return string.Join(" ", keyPairs);
        }

        public override void Run(BuildContext context)
        {
            if (!context.TeamCity().IsRunningOnTeamCity)
            {
                var applicationEnvironment = Environment.GetEnvironmentVariable("Application__Environment");

                var template = context.File("./template.yaml");
                var outputTemplateFile = context.File("./output/packaged_template.yaml");

                // TODO: Get it from metadata
                var applicationSystem = context.Config.ApplicationSystem;
                var applicationSubsystem = context.Config.ApplicationSubsystem;
                var applicationPlatform = context.Config.ApplicationPlatfrom;
                var applicationOwner = context.Config.ApplicationOwner;

                var version = Environment.GetEnvironmentVariable("Application__Version") ?? context.Config.ApplicationVersion;

                Environment.SetEnvironmentVariable("Application__Platform", Environment.GetEnvironmentVariable("Application__Platform") ?? applicationPlatform);
                Environment.SetEnvironmentVariable("Application__Version", version);
                Environment.SetEnvironmentVariable("Application__System", Environment.GetEnvironmentVariable("Application__System") ?? applicationSystem);
                Environment.SetEnvironmentVariable("Application__Subsystem", Environment.GetEnvironmentVariable("Application__Subsystem") ?? applicationSubsystem);
                Environment.SetEnvironmentVariable("Application__Owner", Environment.GetEnvironmentVariable("Application__Owner") ?? applicationOwner);
                Environment.SetEnvironmentVariable("Application__Provisioner", Environment.GetEnvironmentVariable("Application__Provisioner") ?? "cloudformation");
                Environment.SetEnvironmentVariable("Application__AppEnvironment", Environment.GetEnvironmentVariable("Application__AppEnvironment") ?? applicationEnvironment);
                Environment.SetEnvironmentVariable("Application__ApiGatewayDomainCertificateArn", Environment.GetEnvironmentVariable("Application__ApiGatewayDomainCertificateArn") ?? "arn:aws:acm:eu-west-2:637422166946:certificate/f27e2ee0-9cb5-4637-8963-b21542162117");
                Environment.SetEnvironmentVariable("Application__ArtifactsS3Bucket", Environment.GetEnvironmentVariable("Application__ArtifactsS3Bucket") ?? "aws-sam-deployment-artifacts");

                var s3Prefix = string.Join("/", new List<string> { applicationPlatform, applicationSystem, applicationSubsystem, version });
                var stackName = string.Join("-", new List<string> { applicationEnvironment, applicationPlatform, applicationSystem, applicationSubsystem });

                var package = new ProcessArgumentBuilder();

                if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("AWS_PROFILE")))
                {
                    package.AppendSwitch("--profile", Environment.GetEnvironmentVariable("AWS_PROFILE"));
                }

                package.Append("cloudformation")
                   .Append("package")
                   .AppendSwitch("--template-file", template.Path.FullPath)
                   .AppendSwitch("--s3-bucket", Environment.GetEnvironmentVariable("Application__ArtifactsS3Bucket"))
                   .AppendSwitch("--output-template-file", outputTemplateFile.Path.FullPath)
                   .AppendSwitch("--s3-prefix", s3Prefix);

                context.Information("Executing: {0} {1}", "aws", package.Render());
                var exitCode = context.StartProcess("aws", new ProcessSettings { Arguments = package, RedirectStandardOutput = true });

                if (exitCode != 0)
                {
                    throw new ArgumentException("Failed to package artifacts");
                }
                else
                {
                    context.Information($"Successfully packaged artifacts and wrote output template to file {outputTemplateFile}");
                }

                var parametersOverrides = GetParametersOverrides(context);

                var deploy = new ProcessArgumentBuilder();

                if (!string.IsNullOrWhiteSpace(context.Config.AwsProfile))
                {
                    deploy.AppendSwitch("--profile", context.Config.AwsProfile);
                }

                deploy.Append("cloudformation")
                        .Append("deploy")
                        .AppendSwitch("--template-file", outputTemplateFile)
                        .AppendSwitch("--stack-name", stackName)
                        .AppendSwitch("--capabilities", "CAPABILITY_IAM CAPABILITY_NAMED_IAM CAPABILITY_AUTO_EXPAND")
                        .AppendSwitchQuoted("--role-arn", Environment.GetEnvironmentVariable("Application__RoleArn") ?? "arn:aws:iam::637422166946:role/sam-cloudformation-role")
                        .AppendSwitch("--tags", $"Environment={applicationEnvironment} Platform={applicationPlatform} System={applicationSystem} Subsystem={applicationSubsystem} Provisioner={Environment.GetEnvironmentVariable("Application__Provisioner")} Owner={applicationOwner}");

                if (!string.IsNullOrEmpty(parametersOverrides))
                {
                    // Shouldn't be quoted or AWS CLI will fail
                    deploy.AppendSwitch("--parameter-overrides", parametersOverrides);
                }

                context.Information("Executing: {0} {1}", "aws", deploy.Render());
                exitCode = context.StartProcess("aws", new ProcessSettings { Arguments = deploy, RedirectStandardOutput = true });

                if (exitCode != 0)
                {
                    throw new ArgumentException("Failed to deploy stack");
                }
                else
                {
                    context.Information($"Successfully deployed stack {stackName}");
                }
            }
        }

    }
}
