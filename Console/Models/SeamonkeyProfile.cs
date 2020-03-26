using System.Collections.Generic;
using Newtonsoft.Json;
using Octopus.Client.Model;

namespace SeaMonkey.Models
{
    public class SeamonkeyProfile
    {
        public SeamonkeyProfile()
        {
            ProjectVariableSetSizes = new[] {3, 10, 50, 100, 200};
        }
        public int Machines { get; set; }
        public int RolesPerMachine { get; set; }
        public int MachinePolicies { get; set; }
        public int EnvironmentPerGroup { get; set; }
        public int Proxies { get; set; }
        public int UsernamePasswordAccounts { get; set; }
        public int WorkerPools { get; set; }
        public int Workers { get; set; }
        public int ProjectGroups { get; set; }
        public int ProjectsPerGroup { get; set; }
        public int VariablesPerProject { get; set; }
        public int ExtraChannelsPerProject { get; set; }
        public int Tenants { get; set; }

        public int MaxDeploymentsPerProject { get; set; }
        
        public int Teams { get; set; }
        public int Users { get; set; }

        public int Subscriptions { get; set; }

        public int UserRoles { get; set; }

        public int Feeds { get; set; }
        public int ScriptModules { get; set; }
        public int LibraryVariableSets { get; set; }
        public int VariablesPerSet { get; set; }
        public int TenantTagSets { get; set; }
        public int Certificates { get; set; }
        public int[] ProjectVariableSetSizes { get; set; }
    }
}