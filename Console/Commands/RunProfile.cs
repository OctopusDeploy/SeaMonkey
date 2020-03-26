using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Octopus.Client;
using Octopus.Client.Model;
using SeaMonkey.Models;
using SeaMonkey.Monkeys;
using SeaMonkey.ProbabilitySets;
using Serilog;
using Serilog.Core;

namespace SeaMonkey.Commands
{
    [Command("run-profile")]
    public class RunProfile : CommandBase
    {
        // ReSharper disable once InconsistentNaming
        private readonly ILogger logger;
        private string server;
        private string apiKey;
        private string file;
        private string username;
        private string password;

        public RunProfile(ILogger logger)
        {
            this.logger = logger;
            var options = Options.For("Run Profile");
            options.Add("server=", "url of the server", s => this.server = s);
            options.Add("apikey=", "api key to access server", a => this.apiKey = a);
            options.Add("username=", "username", u => this.username = u);
            options.Add("password=", "username", p => this.password = p);
            options.Add("file=", "Output file", f => this.file = f);
        }
        public override Task Execute()
        {
            ValidateParameters();

            var profile = JsonConvert.DeserializeObject<SeamonkeyProfile>(File.ReadAllText(file));
            OctopusRepository repository = GetRepository();
            
            RunMonkeys(repository, profile);
            return Task.CompletedTask;
        }

        private void ValidateParameters()
        {
            if (string.IsNullOrWhiteSpace(server))
                throw new CommandException("server missing");

            if (string.IsNullOrWhiteSpace(apiKey) && (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password)))
                throw new CommandException("apikey or username/password are missing");

            if (string.IsNullOrWhiteSpace(file))
                throw new CommandException("file missing");
        }

        private OctopusRepository GetRepository()
        {
            OctopusRepository repository;
            if (!string.IsNullOrWhiteSpace(apiKey))
            {
                var endpoint = new OctopusServerEndpoint(server, apiKey);
                repository = new OctopusRepository(endpoint);
            }
            else
            {
                var endpoint = new OctopusServerEndpoint(server);
                repository = new OctopusRepository(endpoint);
                repository.Users.SignIn(new LoginCommand {Username = this.username, Password = this.password});
            }

            return repository;
        }

        private void RunMonkeys(OctopusRepository repository, SeamonkeyProfile profile)
        {
            logger.Information("Starting monkey business...");

            logger.Information("Running infrastructure monkey...");
            new InfrastructureMonkey(repository)
                {
                    RolesPerMachine = new StaticValue(profile.RolesPerMachine),
                    EnvironmentsPerGroup = new StaticValue(profile.EnvironmentPerGroup),
                }
                .CreateRecords(
                    profile.MachinePolicies,
                    profile.Proxies,
                    profile.UsernamePasswordAccounts,
                    profile.Machines,
                    profile.WorkerPools,
                    profile.Workers);
            
            logger.Information("Running setup monkey...");
            new SetupMonkey(repository)
            {
                EnvironmentsPerGroup = new StaticValue(profile.EnvironmentPerGroup),
                ProjectsPerGroup = new StaticValue(profile.ProjectsPerGroup),
                VariablesPerProject = new StaticValue(profile.VariablesPerProject),
                ExtraChannelsPerProject = new StaticValue(profile.ExtraChannelsPerProject)
            }.CreateProjectGroups(profile.ProjectGroups);

            new TenantMonkey(repository).Create(profile.Tenants);

            logger.Information("Running deploy monkey...");
            new DeployMonkey(repository).RunForAllProjects(maxNumberOfDeployments: profile.MaxDeploymentsPerProject);

            new ConfigurationMonkey(repository)
                    .CreateRecords(
                        profile.Subscriptions,
                        profile.Teams, 
                        profile.Users, 
                        profile.UserRoles);
            
            logger.Information("Running library monkey...");
            new LibraryMonkey(repository)
                .CreateRecords(
                    profile.Feeds,
                    profile.ScriptModules,
                    profile.LibraryVariableSets,
                    profile.VariablesPerSet,
                    profile.TenantTagSets,
                    profile.Certificates);

            // ReSharper disable once InvertIf
            new VariablesMonkey(repository)
                .CreateVariables(profile.ProjectVariableSetSizes);
        }
    }
}