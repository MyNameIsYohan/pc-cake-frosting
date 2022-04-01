namespace Overleaf.Cake.Frosting.Models
{
    public static class Constants
    {
        public const string TaskGitVersion = "GitVersionTask";
        public const string TaskClean = "CleanTask";
        public const string TaskBuild = "BuildTask";
        public const string TaskSonarInit = "SonarInitTask";
        public const string TaskSonarEnd = "SonarEndTask";
        public const string TaskTestAndCover = "TestAndCoverTask";
        public const string TaskPublish = "PublishTask";
        public const string TaskPackage = "PackageTask";
        public const string TaskVersionFinalize = "VersionFinalizeTask";
        public const string TaskGitTagMaster = "GitTagMaster";
        public const string TaskServerlessDeploy = "ServerlessDeployTask";
        public const string TaskPushToCodeArtifact = "PushToCodeArtifactTask";
        public const string TaskParameterStoreUploader = "ParameterStoreUploaderTask";

        public const string ApplicationEnvironment = "Application__Environment";
        public const string ApplicationVersion = "Application__Version";
        public const string ApplicationUploaderKmsAlias = "Application__UploaderKmsAlias";
        public const string ApplicationUploaderPrivateKeyString = "Application__UploaderPrivateKeyString";

        public const string AwsProfile = "AWS_PROFILE";
        public const string AwsDefaultRegion = "AWS_DEFAULT_REGION";
        public const string AwsRegion = "AWS_REGION";
        public const string AwsAccessKeyId = "AWS_ACCESS_KEY_ID";
        public const string AwsSecretAccessKey = "AWS_SECRET_ACCESS_KEY"; 
        public const string AwsAccountId = "AwsAccountId";

        public const string SonarServerUrl = "SonarQube.ServerUrl";
        public const string SonarServerLogin = "SonarQube.Login";
        public const string SonarProjectName = "SonarQube.ProjectName";
        public const string PullRequestSourceBranch = "PullRequestSourceBranch";
        public const string PullRequestTargetBranch = "PullRequestTargetBranch";
        public const string PullRequestNumber = "PullRequestNumber";

        public const string CodeArtifactNugetPushSkipDuplicates = "CodeArtifactNugetPushSkipDuplicates";
        public const string CodeArtifactAwsAccessKeyPhonixx = "CodeArtifactAwsAccessKeyPhonixx";
        public const string CodeArtifactAwsSecretPhonixx = "CodeArtifactAwsSecretPhonixx";
        public const string CodeArtifactAwsAccessKeyBloxx = "CodeArtifactAwsAccessKeyBloxx";
        public const string CodeArtifactAwsSecretBloxx = "CodeArtifactAwsSecretBloxx";

        public const string GitGenericUserName = "GitGenericUserNameNLBuild";
        public const string GitGenericUserPassword = "GitGenericUserPasswordNLBuild";
    }
}
