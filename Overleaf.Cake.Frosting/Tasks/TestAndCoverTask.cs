using Cake.Common.Build;
using Cake.Common.Tools.DotCover;
using Cake.Common.Tools.DotCover.Cover;
using Cake.Common.Tools.DotCover.Report;
using Cake.Common.Tools.DotNet;
using Cake.Common.Tools.DotNet.Test;
using Cake.Common.Tools.DotNetCore;
using Cake.Core.IO;
using Cake.Frosting;
using Overleaf.Cake.Frosting.Models;
using System;

namespace Overleaf.Cake.Frosting.Tasks
{
    [TaskName(Constants.TaskTestAndCover)]
    public class TestAndCoverTask : FrostingTask<BuildContext>
    {
        public override void Run(BuildContext context)
        {
            const string toolExecutable = "dotcover.exe";
            var coverSettings = new DotCoverCoverSettings { ToolPath = new FilePath($"{context.Tools.Resolve(toolExecutable).GetDirectory()}/{toolExecutable}") };
            coverSettings.Filters.Add("-:Tests");
            coverSettings.Filters.Add("-:build");
            try
            {
                context.DotCoverCover(
                           tool => tool.DotNetTest($"./{context.Config.SolutionFile}", new DotNetTestSettings
                           {
                               Configuration = context.Config.MsBuildConfiguration,
                               NoBuild = true,
                               Verbosity = DotNetCoreVerbosity.Normal
                           }),
                           new FilePath(context.Config.CoverageResult),
                           coverSettings);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                throw;
            }

            finally
            {
                if (context.TeamCity().IsRunningOnTeamCity)
                {
                    context.TeamCity().ImportDotCoverCoverage(context.Config.CoverageResult, context.Tools.Resolve(toolExecutable).GetDirectory());
                }
                context.DotCoverReport(context.Config.CoverageResult, new FilePath(context.Config.CoverageReport), new DotCoverReportSettings
                {
                    ReportType = DotCoverReportType.HTML
                });
            }
        }
    }
}
