using System;
using System.Collections.Generic;
using System.Linq;
using Octopus.Client;
using Octopus.Client.Model;
using SeaMonkey.ProbabilitySets;
using Serilog;

namespace SeaMonkey.Monkeys
{
    // This does not generated any available options for variable scopes (like creating machines/environments/tenant tags etc.
    // So if you want more interesting variable scopes, generate these resources before hand
    public class VariablesMonkey : Monkey
    {
        private static string ProjectGroupName = "Variable Sets";
        private readonly SubsetProbability<string> _variableNameProbability = new SubsetProbability<string>(1, 4,
                new[]
                {
                    "Database", "Web", "Password", "Name", "String", "Connection", "Key", "Api", "Auth", "Token",
                    "Machine", "Id", "Server", "App", "Port", "Site", "Db", "Url", "Environment", "Admin", "Directory",
                    "User", "Location"
                });

        private readonly SubsetProbability<string> _variableDescriptionProbability =
            new SubsetProbability<string>(0, 1000, DescriptionWords());

        private readonly SubsetProbability<char> _variableValueProbability = new SubsetProbability<char>(0, 20, AlphaNumeric());

        private readonly BooleanProbability _shouldPrompt = new BooleanProbability(0.1);
        private readonly BooleanProbability _required = new BooleanProbability(0.5);
        private readonly LinearProbability _maximumNumberOfScopeValues = new LinearProbability(0, 5);
        private readonly BooleanProbability _isStringVariable = new BooleanProbability(0.75);

        public VariablesMonkey(OctopusRepository repository) 
            : base(repository)
        {
        }

        public void CreateVariables(params int[] variableSetSizes)
        {
            var projectGroupResource = new ProjectGroupResource()
            {
                Id = Guid.NewGuid().ToString(),
                Name = ProjectGroupName,
                Description = "Projects with variable sets of varying size, generted by SeaMonkey"
            };

            Log.Information("Creating containing project group");
            if (Repository.ProjectGroups.FindByName(projectGroupResource.Name) == null)
            {
                projectGroupResource = Repository.ProjectGroups.Create(projectGroupResource);
            }

            var lifecycle = Repository.Lifecycles.FindAll().First();

            var numberOfChars = variableSetSizes.Select(s => s.ToString().Length).Max();
            foreach (var variableSetSize in variableSetSizes)
            {
                var project = CreateProject(variableSetSize, numberOfChars, projectGroupResource, lifecycle);
                PopulateVariableSet(project, variableSetSize);
            }
        }

        public void CleanupVariables()
        {
            var projectGroupResource = Repository.ProjectGroups.FindByName(ProjectGroupName);
            var projects = Repository.Projects.GetAll();
            var projectsToDelete = projects.Where(p => p.ProjectGroupId == projectGroupResource.Id);
            foreach (var project in projectsToDelete)
            {
                Log.Information("Deleting project {projectName}", project.Name);
                Repository.Projects.Delete(project);
            }
            Log.Information("Deleting project group {projectGroupName}", projectGroupResource.Name);
            Repository.ProjectGroups.Delete(projectGroupResource);
        }

        private ProjectResource CreateProject(int variableSetSize, int numberOfCharsInVariableSetSizeInTitle, 
            ProjectGroupResource projectGroupResource, LifecycleResource lifecycle)
        {
            var variableSizePadded = variableSetSize.ToString().PadLeft(numberOfCharsInVariableSetSizeInTitle, '0');

            var project = new ProjectResource()
            {
                LifecycleId = lifecycle.Id,
                ProjectGroupId = projectGroupResource.Id,
                Name = $"Variables (Size {variableSizePadded})"
            };
            var p = Repository.Projects.FindByName(project.Name);
            
            if (p != null) return p;
            
            Log.Information("Creating project {projectName}", project.Name);
            return Repository.Projects.Create(project);
        }

        private void PopulateVariableSet(ProjectResource project, int numberOfVariables)
        {
            var variableSet = Repository.VariableSets.Get(project.VariableSetId);
            Log.Information("Generating variables for {projectName}", project.Name);
            var variables = CreateVariablesForVariableSet(variableSet.ScopeValues, numberOfVariables);
            variableSet.Variables = variables;
            Log.Information("Saving variable set for project {projectName}", project.Name);
            Repository.VariableSets.Modify(variableSet);
        }

        private List<VariableResource> CreateVariablesForVariableSet(VariableScopeValues variableSetScopeValues, int numberOfVariables)
        {
            return Enumerable.Range(0, numberOfVariables).Select(i =>
            {
                var variable = new VariableResource()
                {
                    Name = $"{GenerateSomeVariableName()}",
                    IsEditable = true
                };

                bool shouldPrompt = _shouldPrompt.Get();
                if (shouldPrompt)
                {
                    variable.Prompt = new VariablePromptOptions()
                    {
                        Description = GenerateSomeVariableDescription(),
                        Label = GenerateSomeVariableName(),
                        Required = _required.Get()
                    };
                }

                variable.Scope = CreateScopeSpecification(variableSetScopeValues, shouldPrompt);

                // TODO: Add certificates
                // For new, only string and password types are supported
                bool isString = _isStringVariable.Get();
                if (isString)
                {
                    variable.Type = VariableType.String;
                    variable.Value = shouldPrompt ? string.Empty : GenerateSomeVariableValue();
                }
                else
                {
                    variable.Type = VariableType.Sensitive;
                    variable.Value = shouldPrompt ? string.Empty : GenerateSomeVariableValue();
                }

                return variable;
            }).ToList();
        }

        private ScopeSpecification CreateScopeSpecification(VariableScopeValues scopeValues, bool shouldPrompt)
        {
            IEnumerable<string> machines = new string[0];
            IEnumerable<string> actions = new string[0];
            IEnumerable<string> roles = new string[0];
            IEnumerable<string> channels = new string[0];
            IEnumerable<string> tenantTags = new string[0];

            var maxNumberOfScopeValues = _maximumNumberOfScopeValues.Get();

            var environments = new SubsetProbability<ReferenceDataItem>(0, maxNumberOfScopeValues, scopeValues.Environments).Get().Select(x => x.Id);
            if (!shouldPrompt)
            {
                machines = new SubsetProbability<ReferenceDataItem>(0, maxNumberOfScopeValues, scopeValues.Machines).Get().Select(x => x.Id);
                actions = new SubsetProbability<ReferenceDataItem>(0, maxNumberOfScopeValues, scopeValues.Actions).Get().Select(x => x.Id);
                roles = new SubsetProbability<ReferenceDataItem>(0, maxNumberOfScopeValues, scopeValues.Roles).Get().Select(x => x.Id);
                channels = new SubsetProbability<ReferenceDataItem>(0, maxNumberOfScopeValues, scopeValues.Channels).Get().Select(x => x.Id);
                tenantTags = new SubsetProbability<ReferenceDataItem>(0, maxNumberOfScopeValues, scopeValues.TenantTags).Get().Select(x => x.Id);
            }

            return new ScopeSpecification
            {
                {ScopeField.Environment, new ScopeValue(environments)},
                {ScopeField.Machine, new ScopeValue(machines)},
                {ScopeField.Action, new ScopeValue(actions)},
                {ScopeField.Role, new ScopeValue(roles)},
                {ScopeField.Channel, new ScopeValue(channels)},
                {ScopeField.TenantTag, new ScopeValue(tenantTags)},
            };
        }

        public string GenerateSomeVariableName()
        {
            return string.Join("", _variableNameProbability.Get());
        }

        public string GenerateSomeVariableDescription()
        {
            return string.Join("", _variableDescriptionProbability.Get());
        }

        private string GenerateSomeVariableValue()
        {
            return string.Concat(_variableValueProbability.Get());
        }

        private static IEnumerable<string> DescriptionWords()
        {
            var loremIpsum = "Lorem ipsum dolor sit amet, consectetur adipiscing elit. Mauris vulputate dolor in lacus pharetra accumsan. Mauris lectus purus, suscipit ac mauris vel, efficitur volutpat ligula. Integer nec rhoncus lacus. Interdum et malesuada fames ac ante ipsum primis in faucibus. Proin at volutpat tellus, eget porta erat. Morbi non metus porttitor, venenatis velit in, ornare est. Vivamus et molestie orci, vel sodales velit. Sed vestibulum interdum ligula ac pellentesque";
            return loremIpsum.Split(' ');
        }

        private static IEnumerable<char> AlphaNumeric()
        {
            return "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        }
    }
}