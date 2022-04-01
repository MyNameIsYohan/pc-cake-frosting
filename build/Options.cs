using System.Collections.Generic;

namespace Build
{
    public class Options
    {
        public string ApplicationName { get; set; }
        public string ToolsFolder { get; set; }
        public string ArtifactsFolder { get; set; }
        public string CoverageResult { get; set; }
        public string CoverageReport { get; set; }
        public string PublishFolder { get; set; }
        public string SolutionFile { get; set; }

        // for axample "Tests"
        public string TestProjectPattern { get; set; }
        public List<string> PackagingNuspec { get; set; }
        public List<string> DotCoverFilters { get; set; }

        //that should be bitbucket projectName and the repository name in this format "PROJECTNAME_RepoName" for example "NLMICROSERVICES_parkingright-service"
        public string SonarQubeProjectKey { get; set; }

        public string AwsCodeArtifactsRepo { get; set; } = "bloxx";
        public string AwsCodeArtifactsDomain { get; set; } = "prod-automation-codeartifact";
        public string AwsCodeArtifactsDomainOwner { get; set; } = "380786374138";

        public string AwsRegion { get; set; } = "eu-central-1";
    }
}
