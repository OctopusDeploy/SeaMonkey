using System;
using System.Linq;
using System.Threading;
using Octopus.Client;
using Octopus.Client.Model;
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
            //if (args.Length != 2)
              //  throw new ApplicationException("Usage: SeaMonkey.exe <ServerUri> <ApiKey>");

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Information()
                .WriteTo.ColoredConsole()
                .WriteTo.File(@"C:\Temp\SeaMonkey.log")
                .CreateLogger();

            const bool runSetupMonkey = false;
            const bool runTenantMonkey = false;
            const bool runDeployMonkey = true;
            const bool runConfigurationMonkey = false;
            const bool runInfrastructureMonkey = false;
            const bool runLibraryMonkey = false;
            const bool runVariablesMonkey = false;

            var repos = Enumerable.Range(0, 500).AsParallel().WithDegreeOfParallelism(25).Select(i => new {
                Index = i,
                Repository = CreateRepo(i)
            }).ToDictionary(p => p.Index, p => p.Repository);
            

                int round = 0;
                while (true)
                {
                    
                    Log.Information("Starting round {0}", round);
                    Enumerable.Range(0, 500).AsParallel().WithDegreeOfParallelism(50).Select(client =>
                    {
                        if (client >= 500)
                        {
                            Log.Warning("Got {0}", client);
                            return client;
                        }

                     
                        try
                        {
                            
                            
                            Log.Information("Running code for {0} client", client);

                            var repository = repos[client];
                           // var projecExists = repository.Projects.GetAll().Any();
                           // if (projecExists) return i;
                            //var user = repository.Users.GetCurrent();
                            
                            //Log.Information("Running setup for {0}", server);

                            RunMonkeys(repository,
                                runSetupMonkey,
                                runDeployMonkey,
                                runConfigurationMonkey,
                                runInfrastructureMonkey,
                                runLibraryMonkey,
                                runTenantMonkey,
                                runVariablesMonkey);


                            var failed = repository.Tasks.FindAll().Where(t => t.State == TaskState.Failed).ToArray();
                            if (failed.Any()) Log.Warning("The following tasks failed on {0}: {1}", client, failed.Select(t => t.Id));

                            Log.Information("Done running code for {0} server", client);

                            return client;
                        }
                        catch (Exception ex)
                        {
                            Log.Error(ex, "OOPS, something went wrong for {0}", client);
                            return client;
                        }
                    }).ToList();
                    
                    Log.Information("Finished round {0}", round);
                    round++;
                    Thread.Sleep(TimeSpan.FromMinutes(1));
                }
            
            Console.WriteLine("Done. Press any key to exit");
            Console.ReadKey();            
        }

        private static OctopusRepository CreateRepo(int client)
        {                            
            Log.Information("Creating client {0}", client);
            var server = "http://hosted.southcentralus.cloudapp.azure.com/customer" + client.ToString("0000");
            var endpoint = new OctopusServerEndpoint(server);
            var repository = new OctopusRepository(endpoint);
            repository.Users.SignIn("Admin", "Password01)!", true);

            Log.Information("Created client {0}", client);

            return repository;
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
                    StepsPerProject = new LinearProbability(1, 1)
                }.Create(1);
            }

            if (runTenantMonkey)
            {
                new TenantMonkey(repository).Create(200);
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
                    .RunForAllProjects(maxNumberOfDeployments: 1);
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
                    .CreateVariables(3, 10, 50, 100, 200, 250, 300, 400, 500, 1000, 2000, 5000, 10000);
                    //.CleanupVariables();
            }
        }
    }
}
