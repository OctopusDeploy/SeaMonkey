using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Octopus.Client;
using Octopus.Client.Model;
using SeaMonkey.ProbabilitySets;
using Serilog;
using Serilog.Events;

namespace SeaMonkey.Monkeys
{
    public class RunbookMonkey : Monkey
    {
        private readonly Random _rnd = new Random(235346798);

        public RunbookMonkey(OctopusRepository repository) : base(repository)
        {
        }

        public class RunbookInfo
        {
            public ProjectResource Project { get; set; }
            public RunbookResource Runbook { get; set; }
            public ReferenceCollection EnvironmentIds { get; set; }
            public RunbookSnapshotResource LatestRunbookSnapshot { get; set; }
            public RunbookRunResource RunbookRun { get; set; }
            public RunbookStepsResource RunbookSteps { get; set; }
        }

        public BooleanProbability ChanceOfANewRunbookSnapshot { get; set; } = new BooleanProbability(0.25);
        public BooleanProbability ChanceOfAStepsChangeOnNewRunbookSnapshot { get; set; } = new BooleanProbability(0.75);

        public void RunForAllRunbooks(TimeSpan delayBetween = default(TimeSpan), int maxNumberOfRunbookRuns = int.MaxValue)
        {
            RunFor("All Runbooks", () => (prj, env) => true, delayBetween, maxNumberOfRunbookRuns);
        }

        public void RunFor(string description, Func<Func<RunbookInfo, string, bool>> filterFactory, TimeSpan delayBetween = default(TimeSpan), int maxNumberOfRunbookRuns = int.MaxValue)
        {
            var projectInfos = GetRunbookInfos();
            var projectEnvsQ = from p in projectInfos
                               from e in p.EnvironmentIds
                               select new
                               {
                                   RunbookInfo = p,
                                   EnvironmentId = e,
                               };
      
            var projectEnvs = projectEnvsQ.ToArray();

            for (var cnt = 1; cnt <= maxNumberOfRunbookRuns; cnt++)
            {
                var filter = filterFactory();
                var filteredItems = projectEnvs.Where(e => filter(e.RunbookInfo, e.EnvironmentId)).ToArray();
                var item = filteredItems[_rnd.Next(0, filteredItems.Length)];

                if (item.RunbookInfo.LatestRunbookSnapshot == null || ChanceOfANewRunbookSnapshot.Get())
                    CreateRunbookSnapshot(item.RunbookInfo);

                CreateRunbookRun(item.RunbookInfo, item.EnvironmentId);

                Log.Write(cnt % 10 == 0 ? LogEventLevel.Information : LogEventLevel.Verbose,
                    "{description}: {n} runs", description, cnt);
                Thread.Sleep(delayBetween);
            }
        }

        private RunbookInfo[] GetRunbookInfos()
        {
            var projects = Repository.Projects.GetAll();
            var environments = Repository.Environments.FindAll(pathParameters: new { take= int.MaxValue}).ToArray();

            var runbookSnapshots = from r in Repository.RunbookSnapshots.FindAll(pathParameters: new { take = int.MaxValue })
                           let x = new { r.ProjectId, RunbookSnapshot = r, Name = r.Name }
                           group x by x.ProjectId
                           into g
                           select new
                           {
                               ProjectId = g.Key,
                               LatestRunbookSnapshot = g.OrderByDescending(r => r.Name).First().RunbookSnapshot
                           };

            var runbooks = new List<RunbookResource>();
            foreach (var project in projects)
            {
                var projectRunbooks = Repository.Projects.GetAllRunbooks(project);
                runbooks.AddRange(projectRunbooks);
            }

            var runbookSteps = runbooks.AsParallel()
                .WithDegreeOfParallelism(10)
                .Select(p => Repository.RunbookSteps.Get(p.RunbookStepsId))
                .ToArray();

            var q = from p in projects
                    join rb in runbooks on p.Id equals rb.ProjectId
                    join r in runbookSnapshots on p.Id equals r.ProjectId into rj
                    from r in rj.DefaultIfEmpty()
                    select new RunbookInfo
                    {
                        Project = p,
                        Runbook = rb,
                        LatestRunbookSnapshot = r?.LatestRunbookSnapshot,
                        EnvironmentIds = new ReferenceCollection(environments.Select(e => e.Id)),
                        RunbookSteps = runbookSteps.First(c => c.RunbookId == rb.Id)
                    };

            return q.ToArray();
        }


        private void CreateRunbookSnapshot(RunbookInfo projectInfo)
        {
            if (ChanceOfAStepsChangeOnNewRunbookSnapshot.Get())
                projectInfo.RunbookSteps = UpdateRunbookSteps(projectInfo.Runbook);

            var release = new RunbookSnapshotResource()
            {
                ProjectId = projectInfo.Project.Id,
                RunbookId = projectInfo.Runbook.Id,
                Name = GetNextRunbookSnapshotNumber(projectInfo),
                SelectedPackages = projectInfo.RunbookSteps
                    .Steps
                    .SelectMany(s => s.Actions)
                    .Where(a => a.Properties.ContainsKey("Octopus.Action.Package.NuGetPackageId"))
                    .Select(a => new SelectedPackage(a.Name, "3.2.4"))
                    .ToList()
            };
            projectInfo.LatestRunbookSnapshot = Repository.RunbookSnapshots.Create(release);
        }

        private string GetNextRunbookSnapshotNumber(RunbookInfo projectInfo)
        {
            return Guid.NewGuid().ToString();
        }

        public void CreateRunbookRun(RunbookInfo projectInfo, string environmentId)
        {
            Repository.RunbookRuns.Create(new RunbookRunResource()
            {
                ProjectId = projectInfo.Project.Id,
                RunbookId = projectInfo.Runbook.Id,
                RunbookSnapshotId = projectInfo.LatestRunbookSnapshot.Id,
                EnvironmentId = environmentId,
                ForcePackageRedeployment = true
            });
        }
    }

}