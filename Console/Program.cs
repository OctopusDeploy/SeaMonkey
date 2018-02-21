using System;
using Octopus.Client;
using Octopus.Client.Model;
using SeaMonkey.Monkeys;
using SeaMonkey.ProbabilitySets;
using Serilog;

namespace SeaMonkey
{
    class Program
    {
        public static readonly Random Rnd = new Random(235346798);

        static void Main(string[] args)
        {
            Random rnd = new Random();

            if (args.Length != 1)
            {
                throw new ApplicationException("Usage: SeaMonkey.exe <address>");
            }

            var address = args[0];
            //var server = args[0];
           // var apikey = args[1];
            //const string apikey = "API-GCCFRMSJ53TA9S9RN3SPW2UOPA8";
            const bool runSetupMonkey = false;
            const bool runDeployMonkey = true;
            const bool runConfigurationMonkey = false;
            const bool runInfrastructureMonkey = false;
            const bool runLibraryMonkey = false;
            const bool runTenantMonkey = false;

            try
            {
                Log.Logger = new LoggerConfiguration()
                    .MinimumLevel.Information()
                    .WriteTo.ColoredConsole()
                    .CreateLogger();

                var endpoint = new OctopusServerEndpoint(address);
                var repository = new OctopusRepository(endpoint);
                repository.Users.SignIn("admin", "Passw0rd123");


                SetLicence(repository);
                    
                    
                RunMonkeys(repository,
                    runSetupMonkey,
                    runDeployMonkey,
                    runConfigurationMonkey,
                    runInfrastructureMonkey,
                    runLibraryMonkey,
                    runTenantMonkey);
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

        private static void SetLicence(OctopusRepository repository)
        {
            	repository.Client.Put($"/api/licenses/licenses-current", new LicenseResource()
            	{
            		LicenseText = @"<License Signature=""HMGTCu29UUaVZas2JaublWaYUvBs/IUI3P8agy1tnfiOI7EyH/HauJnnb9o60F9jrztzFGXBW5FLbR3COJ3Kow=="">
	                <LicensedTo>RW Hosted Load Testing</LicensedTo>
	                <LicenseKey>54633-85092-86428-58812</LicenseKey>
	                <Version>2.0<!-- License Schema Version --></Version>
	                <ValidFrom>2018-02-18</ValidFrom>
	                <MaintenanceExpires>2020-02-18</MaintenanceExpires>
	                <ProjectLimit>Unlimited</ProjectLimit>
	                <MachineLimit>Unlimited</MachineLimit>
	                <UserLimit>Unlimited</UserLimit>
	                </License>"
            	});
            	
        }

        static void RunMonkeys(OctopusRepository repository,
            bool runSetupMonkey,
            bool runDeployMonkey,
            bool runConfigurationMonkey,
            bool runInfrastructureMonkey,
            bool runLibraryMonkey,
            bool runTenantMonkey)
        {
            Console.WriteLine("Starting monkey business...");

            if (runSetupMonkey)
            {
                Console.WriteLine("Running setup monkey...");
                //new SetupMonkey(repository).CreateTenants(500);
                new SetupMonkey(repository)
                {
                    StepsPerProject = new LinearProbability(1, 10)
                }.CreateProjectGroups(5);
            }

            if (runTenantMonkey)
            {
                new TenantMonkey(repository).Create(100);
            }

            if (runDeployMonkey)
            {
                Console.WriteLine("Running deploy monkey...");
                //new DeployMonkey(repository).RunForGroup(SetupMonkey.TenantedGroupName, 5000);
                new DeployMonkey(repository)
                    .RunForAllProjects(maxNumberOfDeployments: 1000);
            }

            if (runConfigurationMonkey)
            {
                Console.WriteLine("Running configuration monkey...");
                new ConfigurationMonkey(repository)
                    .CreateRecords(70, 70, 70, 70);
            }

            if (runInfrastructureMonkey)
            {
                Console.WriteLine("Running infrastructure monkey...");
                new InfrastructureMonkey(repository)
                    .CreateRecords(70, 70, 70, 70);
            }

            if (runLibraryMonkey)
            {
                Console.WriteLine("Running library monkey...");
                new LibraryMonkey(repository)
                    .CreateRecords(70, 70, 70, 70);
            }

        }
    }
}
