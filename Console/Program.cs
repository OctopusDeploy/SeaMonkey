using System;
using Octopus.Client;
using SeaMonkey.Monkeys;
using SeaMonkey.ProbabilitySets;
using Serilog;

namespace SeaMonkey
{
    internal class Program
    {
        public static readonly Random Rnd = new Random(235346798);

        private static void Main(string[] args)
        {
            if (args.Length != 9)
                throw new ApplicationException("Usage: SeaMonkey.exe <ServerUri> <ApiKey> <RunSetupMonkey> <RunTenantMonkey> <RunDeployMonkey> <RunConfigurationMonkey> <RunInfrastructureMonkey> <RunLibraryMonkey> <RunVariablesMonkey>");

            var server = args[0];
            var apiKey = args[1];
            var runSetupMonkey = args[2].ToLower() == "true";
            var runTenantMonkey = args[3].ToLower() == "true";
            var runDeployMonkey = args[4].ToLower() == "true";
            var runConfigurationMonkey = args[5].ToLower() == "true";
            var runInfrastructureMonkey = args[6].ToLower() == "true";
            var runLibraryMonkey = args[7].ToLower() == "true";
            var runVariablesMonkey = args[8].ToLower() == "true";

            try
            {
                Log.Logger = new LoggerConfiguration()
                    .MinimumLevel.Information()
                    .WriteTo.LiterateConsole()
                    .CreateLogger();

                var endpoint = new OctopusServerEndpoint(server, apiKey);
                var repository = new OctopusRepository(endpoint);
                RunMonkeys(repository,
                    runSetupMonkey,
                    runDeployMonkey,
                    runConfigurationMonkey,
                    runInfrastructureMonkey,
                    runLibraryMonkey,
                    runTenantMonkey,
                    runVariablesMonkey);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "OOPS");
            }
            finally
            {
                Console.WriteLine("Done. Press any key to exit");
                Console.ReadKey();
            }
        }

        private static void RunMonkeys(OctopusRepository repository,
            bool runSetupMonkey,
            bool runDeployMonkey,
            bool runConfigurationMonkey,
            bool runInfrastructureMonkey,
            bool runLibraryMonkey,
            bool runTenantMonkey, 
            bool runVariablesMonkey)
        {
            Console.WriteLine("Starting monkey business...");

            if (runSetupMonkey)
            {
                Console.WriteLine("Running setup monkey...");
                //new SetupMonkey(repository).CreateTenants(500);
                new SetupMonkey(repository)
                {
                    StepsPerProject = new LinearProbability(1, 3)
                }.CreateProjectGroups(10);
            }

            if (runTenantMonkey)
            {
                new TenantMonkey(repository).Create(50);
            }

            if (runInfrastructureMonkey)
            {
                Console.WriteLine("Running infrastructure monkey...");
                new InfrastructureMonkey(repository)
                    .CreateRecords(7, 7, 7, 70, 2, 2);
            }

            if (runDeployMonkey)
            {
                Console.WriteLine("Running deploy monkey...");
                //new DeployMonkey(repository).RunForGroup(SetupMonkey.TenantedGroupName, 5000);
                new DeployMonkey(repository)
                    .RunForAllProjects(maxNumberOfDeployments: 100);
            }

            if (runConfigurationMonkey)
            {
                Console.WriteLine("Running configuration monkey...");
                new ConfigurationMonkey(repository)
                    .CreateRecords(70, 70, 70, 70);
            }

            if (runLibraryMonkey)
            {
                Console.WriteLine("Running library monkey...");
                new LibraryMonkey(repository)
                    .CreateRecords(70, 70, 10, 3, 70, 50);
            }

            // ReSharper disable once InvertIf
            if (runVariablesMonkey)
            {
                Console.WriteLine("Running variables monkey...");
                new VariablesMonkey(repository)
                    .CreateVariables(3, 10, 50, 100, 200);
                    //.CleanupVariables();
            }
        }
    }
}
