using System.Collections.Generic;
using Octopus.Client.Model;

namespace SeaMonkey.Models
{
    public class SeamonkeyProfile
    {
        public ProjectGroupProfile ProjectGroups { get; set; } = new ProjectGroupProfile();
        public InfrastructureProfile Infrastructure { get; set; } = new InfrastructureProfile();
        public LibraryProfile Library { get; set; } = new LibraryProfile();
        public ProfileWithCount Tenants { get; set; } = new ProfileWithCount();
    }

    public class ProfileWithCount
    {
        public int Count { get; set; }
    }
    
    public class ProjectGroupProfile : ProfileWithCount
    {
        public ProjectProfile Projects { get; set; } = new ProjectProfile();
    }

    public class ProjectProfile : ProfileWithCount
    {
        public ProfileWithCount Steps { get; set; } = new ProfileWithCount();
    }

    public class InfrastructureProfile
    {
        public EnvironmentProfile Environments { get; set; } = new EnvironmentProfile();
        public ProfileWithCount Proxies { get; set; } = new ProfileWithCount();
        public ProfileWithCount UsernamePasswordAccounts { get; set; } = new ProfileWithCount();
        public WorkerPoolProfile WorkerPool { get; set; } = new WorkerPoolProfile();
    }

    public class WorkerPoolProfile : ProfileWithCount
    {
        public ProfileWithCount Workers { get; set; } = new ProfileWithCount();
    }
    
    public class EnvironmentProfile
    {
        public MachineProfile Machines { get; set; } = new MachineProfile();
    }

    public class MachineProfile : ProfileWithCount
    {
        public ProfileWithCount RolesPerMachine { get; set; } = new ProfileWithCount();
    }

    public class LibraryProfile
    {
        public ProfileWithCount Feeds { get; set; } = new ProfileWithCount();
        public ProfileWithCount TenantTagSets { get; set; } = new ProfileWithCount();
        public ProfileWithCount ScriptModules { get; set; } = new ProfileWithCount();
        public ProfileWithCount Certificates { get; set; } = new ProfileWithCount();
        public LibraryVariableSetProfile LibraryVariableSetProfile { get; set; } = new LibraryVariableSetProfile();
    }

    public class LibraryVariableSetProfile : ProfileWithCount
    {
        public ProfileWithCount Variables { get; set; } = new ProfileWithCount();
    }
}