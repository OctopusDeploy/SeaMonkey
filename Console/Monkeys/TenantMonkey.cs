using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Octopus.Client;
using Octopus.Client.Model;
using SeaMonkey.ProbabilitySets;
using Serilog;

namespace SeaMonkey.Monkeys
{
    public class TenantMonkey : Monkey
    {
        public TenantMonkey(OctopusRepository repository) : base(repository)
        {
        }

        public int MaxTenantedProjects = 10;

        public IntProbability ProjectsPerTenant { get; set; } = new LinearProbability(1, 10);

        public IntProbability EnvironmentsPerProjectLink { get; set; } = new LinearProbability(0, 4);


        public void Create(int numberOfTenants)
        {
            var projects = GetProjects();
            var lifecycleIndex = Repository.Lifecycles.FindAll().ToDictionary(t => t.Id);
            var allTenants = Repository.Tenants.GetAll();

            Log.Information("Creating {n} tenants", numberOfTenants);

            for (var t = allTenants.Count + 1; t <= numberOfTenants; t++)
            {
                CreateTenant(projects, lifecycleIndex, t);
            }
        }

        List<ProjectResource> GetProjects()
        {
            var currentProjects = Repository.Projects.GetAll();

            var tenantableProjects = currentProjects.Where(t => t.TenantedDeploymentMode != TenantedDeploymentMode.Untenanted).ToList();
            if (tenantableProjects.Count >= MaxTenantedProjects)
                return tenantableProjects;

            var projectsAllowedToBecomeTenanted = currentProjects
                .Where(t => t.TenantedDeploymentMode == TenantedDeploymentMode.Untenanted)
                .TakeRandomSubset(MaxTenantedProjects - tenantableProjects.Count);

            return tenantableProjects.Concat(projectsAllowedToBecomeTenanted).ToList();
        }

        private void CreateTenant(List<ProjectResource> currentProjects, Dictionary<string, LifecycleResource> lifecycleIndex, int t)
        {
            var projects = currentProjects
                .TakeRandomSubset(ProjectsPerTenant.Get())
                .ToList();

            EnsureProjectsTenanted(projects);

            var tenantName = "Tenant " + t.ToString("000");

            var tenantBuilder = Repository.Tenants.CreateOrModify(tenantName);
            tenantBuilder.ClearProjects();
            tenantBuilder.Instance.ProjectEnvironments = projects.ToDictionary(p => p.Id, p => GetEnvironmentsForProject(p, lifecycleIndex));
            tenantBuilder.Save();
            Log.Information("Created tenant {name}", tenantName);
        }

        private void EnsureProjectsTenanted(List<ProjectResource> projects)
        {
            foreach (var untenantedProject in projects.Where(p => p.TenantedDeploymentMode == TenantedDeploymentMode.Untenanted)
            )
            {
                untenantedProject.TenantedDeploymentMode = TenantedDeploymentMode.TenantedOrUntenanted;
                Repository.Projects.Modify(untenantedProject);
            }
        }


        ReferenceCollection GetEnvironmentsForProject(ProjectResource project, IDictionary<string, LifecycleResource> lifecycleIndex)
        {
            return new ReferenceCollection(lifecycleIndex[project.LifecycleId]
                .Phases.SelectMany(phase => phase.AutomaticDeploymentTargets.Concat(phase.OptionalDeploymentTargets))
                .TakeRandomSubset(EnvironmentsPerProjectLink.Get()));
        }
    }


    public static class Extendsions
    {
        public static IEnumerable<T> TakeRandomSubset<T>(this IEnumerable<T> elements, int countToTake)
        {
            var internalList = elements.ToList();
            countToTake = Math.Min(countToTake, internalList.Count);

            var selected = new List<T>();
            for (var i = 0; i < countToTake; ++i)
            {
                var next = Program.Rnd.Next(0, internalList.Count - selected.Count);
                selected.Add(internalList[next]);
                internalList[next] = internalList[internalList.Count - selected.Count];
            }
            return selected;
        }
    }
}
