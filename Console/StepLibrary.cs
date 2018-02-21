using System;
using Octopus.Client.Model;

namespace SeaMonkey
{
    public static class StepLibrary
    {
        public static readonly Func<int, DeploymentStepResource>[] StepFactories = {
            GetSimpleScriptStep,
            GetPackageDeploymentStep,
            GetLargeStep
        };

        private static DeploymentStepResource GetSimpleScriptStep(int id)
        {
            var step = new DeploymentStepResource()
            {
                Name = "Script " + id
            };
            step.Actions.Add(new DeploymentActionResource()
            {
                Name = "Script" + id,
                ActionType = "Octopus.Script"
            });

            step.Actions[0].Properties.Clear();
            step.Actions[0].Properties["Octopus.Action.Script.ScriptSource"] = "Inline";
            step.Actions[0].Properties["Octopus.Action.Script.ScriptBody"] = @"Write-Host 'Hello World'
            Start-Sleep 1";
            step.Properties["Octopus.Action.TargetRoles"] = "InstallStuff";
            return step;
        }

        private static DeploymentStepResource GetLargeStep(int id)
        {
            var step = new DeploymentStepResource()
            {
                Name = "Script " + id
            };
            step.Actions.Add(new DeploymentActionResource()
            {
                Name = "Script" + id,
                ActionType = "Octopus.Script"
            });

            step.Actions[0].Properties.Clear();
            step.Actions[0].Properties["Octopus.Action.Script.ScriptSource"] = "Inline";
            step.Actions[0].Properties["Octopus.Action.Script.ScriptBody"] = @"$OctopusParameters.GetEnumerator() | % { $_.Key + '=' + $_.Value }
            Start-Sleep 1";
            for(int x = 1; x < 20; x++)
                step.Actions[0].Properties[$"FillerProperty{x:000}"] = AReallyBigString;
            step.Properties["Octopus.Action.TargetRoles"] = "InstallStuff";
            return step;
        }

        private static DeploymentStepResource GetPackageDeploymentStep(int id)
        {
            var step = new DeploymentStepResource()
            {
                Name = "Web " + id
            };
            step.Actions.Add(new DeploymentActionResource()
            {
                Name = "Web" + id,
                ActionType = "Octopus.TentaclePackage"
            });

            step.Actions[0].Properties.Clear();
            step.Actions[0].Properties["Octopus.Action.EnabledFeatures"] = "Octopus.Features.ConfigurationTransforms,Octopus.Features.ConfigurationVariables";
            step.Actions[0].Properties["Octopus.Action.Package.AutomaticallyRunConfigurationTransformationFiles"] = "True";
            step.Actions[0].Properties["Octopus.Action.Package.AutomaticallyUpdateAppSettingsAndConnectionStrings"] = "True";
            step.Actions[0].Properties["Octopus.Action.Package.DownloadOnTentacle"] = "False";
            step.Actions[0].Properties["Octopus.Action.Package.NuGetFeedId"] = "feeds-builtin";
            step.Actions[0].Properties["Octopus.Action.Package.NuGetPackageId"] = "Acme.Web";
            step.Properties["Octopus.Action.TargetRoles"] = "InstallStuff";
            return step;
        }


        public static DeploymentStepResource Random(int id)
            => StepFactories[Program.Rnd.Next(0, StepFactories.Length)](id);


        private const string AReallyBigString =
            @"Bring a spring upon her cable flogging parley bilge rat port broadside Sea Legs gaff. Hulk skysail fathom six pounders reef sails rigging black jack pillage. Aye dance the hempen jig draught keel spyglass chase guns galleon red ensign. Booty doubloon piracy interloper careen hempen halter aye man-of-war.
Execution dock Davy Jones' Locker pillage hempen halter draught galleon sutler bring a spring upon her cable. Quarter Jack Ketch lanyard pink knave skysail Corsair red ensign. Transom American Main Gold Road spanker dead men tell no tales grog furl sheet. Spyglass draft prow cackle fruit scuttle Pirate Round Sink me shrouds.
Cog pressgang squiffy prow lad jury mast lugger skysail. Splice the main brace clap of thunder code of conduct crimp log rutters Plate Fleet Yellow Jack. Mutiny doubloon fathom avast Buccaneer skysail blow the man down crimp. Quarter yawl shrouds American Main measured fer yer chains crow's nest heave to Privateer.
Boatswain Plate Fleet aft gangplank barque mizzenmast lateen sail topgallant. Fore hang the jib pink walk the plank scuppers gibbet parley carouser. Gaff gangplank grog blossom driver cackle fruit gabion spanker broadside. Plate Fleet strike colors swing the lead long clothes Arr Sea Legs quarterdeck booty.
Lookout loaded to the gunwalls scourge of the seven seas draft pink lad tender Corsair. Walk the plank deadlights to go on account carouser coffer barkadeer jack aye. Ahoy doubloon square-rigged main sheet aye Cat o'nine tails hang the jib hardtack. Boom Nelsons folly hearties Gold Road bilged on her anchor knave scurvy snow.
Lass broadside scourge of the seven seas bilged on her anchor Yellow Jack gibbet Jack Ketch deadlights. Galleon careen avast bilge rat list starboard shrouds trysail. Gold Road rigging scourge of the seven seas maroon pressgang crimp fathom wench. Hulk boatswain mutiny Sea Legs furl killick skysail warp.
Trysail interloper line prow bilge broadside keelhaul loot. Avast square-rigged chantey Spanish Main grapple yo-ho-ho haul wind booty. Log gun Admiral of the Black chandler hornswaggle Plate Fleet chase guns plunder. No prey, no pay interloper Barbary Coast gun salmagundi pillage gibbet cog.
Ye black jack main sheet ahoy lee mutiny loot topmast. Gun smartly Sea Legs gibbet fluke shrouds nipper case shot. Squiffy sutler barque league bowsprit run a rig matey rutters. Corsair yardarm lugsail topmast take a caulk pressgang galleon keel.
Quarterdeck capstan gun splice the main brace nipper scuttle bowsprit gabion. Black spot lee lanyard mizzen heave to rutters come about crimp. Weigh anchor knave boatswain man-of-war landlubber or just lubber chandler ballast lee. Bowsprit mizzenmast galleon sloop gangplank Pieces of Eight aft code of conduct.
Fore swab league code of conduct crack Jennys tea cup nipperkin grapple Sail ho. Cable list transom ahoy take a caulk square-rigged six pounders crimp. Black spot prow bucko list reef sails Privateer hogshead marooned. Jib careen skysail lass chase capstan hang the jib line.";
    }
}