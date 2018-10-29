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
    public class DeployMonkey : Monkey
    {
        private readonly Random _rnd = new Random(235346798);

        public DeployMonkey(OctopusRepository repository) : base(repository)
        {

        }


        public class ProjectInfo
        {
            public ProjectGroupResource ProjectGroup { get; set; }
            public ProjectResource Project { get; set; }
            public ReleaseResource LatestRelease { get; set; }
            public ReferenceCollection EnvironmentIds { get; set; }
            public IList<ChannelResource> Channels { get; set; }
            public DeploymentProcessResource DeploymentProcess { get; set; }
        }

        public BooleanProbability ChanceOfANewRelease { get; set; } = new BooleanProbability(0.25);
        public BooleanProbability ChanceOfAProcessChangeOnNewRelease { get; set; } = new BooleanProbability(0.75);

        public void RunForAllProjects(TimeSpan delayBetween = default(TimeSpan), int maxNumberOfDeployments = int.MaxValue)
        {
            RunFor("All Projects", () => (prj, env) => true, delayBetween, maxNumberOfDeployments);
        }

        public void RunForProject(string name, TimeSpan delayBetween = default(TimeSpan), int maxNumberOfDeployments = int.MaxValue)
        {
            RunFor(name, () => (prj, env) => prj.Project.Name == name, delayBetween, maxNumberOfDeployments);
        }

        public void RunForGroup(string name, TimeSpan delayBetween = default(TimeSpan), int maxNumberOfDeployments = int.MaxValue)
        {
            var group = Repository.ProjectGroups.FindByName(name);
            RunFor(name, () => (prj, env) => prj.Project.ProjectGroupId == group.Id, delayBetween, maxNumberOfDeployments);
        }

        public void RunFor(string description, Func<Func<ProjectInfo, string, bool>> filterFactory, TimeSpan delayBetween = default(TimeSpan), int maxNumberOfDeployments = int.MaxValue)
        {
            var projectInfos = GetProjectInfos();
            var projectEnvsQ = from p in projectInfos
                               from e in p.EnvironmentIds
                               select new
                               {
                                   ProjectInfo = p,
                                   EnvironmentId = e,
                               };
      
            var projectEnvs = projectEnvsQ.ToArray();

            for (var cnt = 1; cnt <= maxNumberOfDeployments; cnt++)
            {
                var filter = filterFactory();
                var filteredItems = projectEnvs.Where(e => filter(e.ProjectInfo, e.EnvironmentId)).ToArray();
                var item = filteredItems[_rnd.Next(0, filteredItems.Length)];

                //if (item.ProjectInfo.LatestRelease == null || ChanceOfANewRelease.Get())
                CreateRelease(item.ProjectInfo);

                CreateDeployment(item.ProjectInfo, item.EnvironmentId);

                Log.Write(cnt % 10 == 0 ? LogEventLevel.Information : LogEventLevel.Verbose, "{description}: {n} deployments", description, cnt);
               // Thread.Sleep(delayBetween);
            }
        }

        public void RunOncePerProjectForGroup(string name, TimeSpan delayBetween = default(TimeSpan))
        {
            var group = Repository.ProjectGroups.FindByName(name);
            RunOncePerProjectFor(name, () => (prj, env) => prj.Project.ProjectGroupId == group.Id, delayBetween);
        }

        public void RunOncePerProjectFor(string description, Func<Func<ProjectInfo, string, bool>> filterFactory,
            TimeSpan delayBetween = default(TimeSpan))
        {
            var projectInfos = GetProjectInfos();
            var projectEnvsQ = from p in projectInfos
                from e in p.EnvironmentIds
                select new
                {
                    ProjectInfo = p,
                    EnvironmentId = e,
                };

            var projectEnvs = projectEnvsQ.ToArray();

            var filter = filterFactory();
            var filteredItems = projectEnvs.Where(e => filter(e.ProjectInfo, e.EnvironmentId)).ToArray();


            var cnt = 0;
            filteredItems.AsParallel()
                .WithDegreeOfParallelism(10)
                .ForAll(item =>
                {
                    if (item.ProjectInfo.LatestRelease == null || ChanceOfANewRelease.Get())
                        CreateRelease(item.ProjectInfo);

                    CreateDeployment(item.ProjectInfo, item.EnvironmentId);

                    Interlocked.Increment(ref cnt);
                    Log.Write(cnt % 10 == 0 ? LogEventLevel.Information : LogEventLevel.Verbose,
                        "{description}: {n} deployments", description, cnt);
                    Thread.Sleep(delayBetween);
                });
        }

        private ProjectInfo[] GetProjectInfos()
        {
            var projects = Repository.Projects.GetAll();
            var lifecycles = Repository.Lifecycles.FindAll(pathParameters: new { take= int.MaxValue}).ToArray();

            var releases = from r in Repository.Releases.FindAll(pathParameters: new { take = int.MaxValue })
                           let x = new { r.ProjectId, Release = r, Version = SemanticVersion.Parse(r.Version) }
                           group x by x.ProjectId
                           into g
                           select new
                           {
                               ProjectId = g.Key,
                               LatestRelease = g.OrderByDescending(r => r.Version).First().Release
                           };

            var channels = Repository.Channels.FindAll(pathParameters: new {take = int.MaxValue});
            var groups = Repository.ProjectGroups.GetAll();

            var processes = projects.AsParallel()
                .WithDegreeOfParallelism(10)
                .Select(p => Repository.DeploymentProcesses.Get(p.DeploymentProcessId))
                .ToArray();

            var q = from p in projects
                    join r in releases on p.Id equals r.ProjectId into rj
                    from r in rj.DefaultIfEmpty()
                    join l in lifecycles on p.LifecycleId equals l.Id
                    select new ProjectInfo
                    {
                        Project = p,
                        ProjectGroup = groups.First(g => g.Id == p.ProjectGroupId),
                        LatestRelease = r?.LatestRelease,
                        EnvironmentIds = l.Phases.FirstOrDefault()?.OptionalDeploymentTargets ?? new ReferenceCollection(),
                        Channels = channels.Where(c => c.ProjectId == p.Id).ToList(),
                        DeploymentProcess = processes.First(c => c.ProjectId == p.Id)
                    };

            return q.ToArray();
        }


        private void CreateRelease(ProjectInfo projectInfo)
        {
            if (ChanceOfAProcessChangeOnNewRelease.Get())
                projectInfo.DeploymentProcess = UpdateDeploymentProcess(projectInfo.Project);

            var release = new ReleaseResource()
            {
                ChannelId = projectInfo.Channels[Program.Rnd.Next(0, projectInfo.Channels.Count)].Id,
                ProjectId = projectInfo.Project.Id,
                Version = GetNextReleaseNumber(projectInfo),
                SelectedPackages = projectInfo.DeploymentProcess
                    .Steps
                    .SelectMany(s => s.Actions)
                    .Where(a => a.Properties.ContainsKey("Octopus.Action.Package.NuGetPackageId"))
                    .Select(a => new SelectedPackage(a.Name, "3.2.4"))
                    .ToList()
            };
            projectInfo.LatestRelease = Repository.Releases.Create(release);
        }

        private string GetNextReleaseNumber(ProjectInfo projectInfo)
        {
            if (projectInfo.LatestRelease == null)
                return "1.0.0";

            var version = SemanticVersion.Parse(projectInfo.LatestRelease.Version).Version;
            var x = _rnd.Next(100);
            if (x == 0)
                version = new Version(version.Major + 1, 0, 0, 0);
            else if (x < 20)
                version = new Version(version.Major, version.Minor + 1, 0);
            else
                version = new Version(version.Major, version.Minor, version.Build + 1);
            return version.ToString(3);
        }

        public void CreateDeployment(ProjectInfo projectInfo, string environmentId)
        {
            var deployment = Repository.Deployments.Create(new DeploymentResource()
            {
                ProjectId = projectInfo.Project.Id,
                ReleaseId = projectInfo.LatestRelease.Id,
                EnvironmentId = environmentId,
                ForcePackageRedeployment = true
            });

            //var deploymentTask = Repository.Tasks.Get(deployment.TaskId);
           // Thread.Sleep(TimeSpan.FromSeconds(60));
           // Repository.Tasks.WaitForCompletion(deploymentTask, 5, 5);
        }
    }

}