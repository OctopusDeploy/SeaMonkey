using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;

namespace SeaMonkeyUI
{
    public partial class MainWindow : Window
    {
        private SeaMonkeyProps Model { get; }
        private Process ConsoleProcess { get; set; }

        public MainWindow()
        {
            InitializeComponent();

            // You can override these defaults by providing command-line args to the application.
            Model = new SeaMonkeyProps()
            {
                ServerUrl = "http://localhost:8065",
                ServerApiKey = "API-",
                RunSetupMonkey = false,
                RunConfigurationMonkey = false,
                RunDeployMonkey = false,
                RunInfrastructureMonkey = false,
                RunLibraryMonkey = false,
                RunVariableMonkey = false,
                RunTenantMonkey = false,
                RunRunbookMonkey = false,
            };

            var args = Environment.GetCommandLineArgs();
            // ReSharper disable once InvertIf
            if (args.Length == 11)
            {
                // args[0] is reserved for the exe itself.
                Model.ServerUrl = args[1];
                Model.ServerApiKey = args[2];
                Model.RunSetupMonkey = args[3].ToLower() == "true";
                Model.RunTenantMonkey = args[4].ToLower() == "true";
                Model.RunDeployMonkey = args[5].ToLower() == "true";
                Model.RunConfigurationMonkey = args[6].ToLower() == "true";
                Model.RunInfrastructureMonkey = args[7].ToLower() == "true";
                Model.RunLibraryMonkey = args[8].ToLower() == "true";
                Model.RunVariableMonkey = args[9].ToLower() == "true";
                Model.RunRunbookMonkey = args[10].ToLower() == "true";
            }

            FormLoaded();
        }
        
        private void FormLoaded()
        {
            txtServer.Text = Model.ServerUrl;
            txtApiKey.Text = Model.ServerApiKey;
            runSetup.IsChecked = Model.RunSetupMonkey;
            runConfiguration.IsChecked = Model.RunConfigurationMonkey;
            runDeploy.IsChecked = Model.RunDeployMonkey;
            runInfrastructure.IsChecked = Model.RunInfrastructureMonkey;
            runLibrary.IsChecked = Model.RunLibraryMonkey;
            runTenant.IsChecked = Model.RunTenantMonkey;
            runVariables.IsChecked = Model.RunVariableMonkey;
            runRunbooks.IsChecked = Model.RunRunbookMonkey;
        }

        protected override void OnClosed(EventArgs e)
        {
            ConsoleProcess?.CloseMainWindow();
            base.OnClosed(e);
        }

        #region Handlers

        private void BtnSubmit_Click(object sender, RoutedEventArgs e)
        {
            var startInfo = new ProcessStartInfo("SeaMonkey.exe")
            {
                WorkingDirectory = GetSeaMonkeyConsoleExePath(),
                Arguments = $"{this.Model.ServerUrl} {this.Model.ServerApiKey} {this.Model.RunSetupMonkey} {this.Model.RunTenantMonkey} {this.Model.RunDeployMonkey} {this.Model.RunConfigurationMonkey} {this.Model.RunInfrastructureMonkey} {this.Model.RunLibraryMonkey} {this.Model.RunVariableMonkey} {this.Model.RunRunbookMonkey}"
            };
            using (ConsoleProcess = Process.Start(startInfo))
            {
                ConsoleProcess?.WaitForExit();
            }
        }

        private void TxtApiKey_TextChanged(object sender, TextChangedEventArgs e)
        {
            this.Model.ServerApiKey = txtApiKey.Text;
        }

        private void TxtServer_TextChanged(object sender, TextChangedEventArgs e)
        {
            this.Model.ServerUrl = txtServer.Text;
        }

        private void RunSetup_Checked(object sender, RoutedEventArgs e)
        {
            this.Model.RunSetupMonkey = runSetup.IsChecked ?? false;
        }

        private void RunTenant_Checked(object sender, RoutedEventArgs e)
        {
            this.Model.RunTenantMonkey = runTenant.IsChecked ?? false;
        }

        private void RunDeploy_Checked(object sender, RoutedEventArgs e)
        {
            this.Model.RunDeployMonkey = runDeploy.IsChecked ?? false;
        }

        private void RunConfiguration_Checked(object sender, RoutedEventArgs e)
        {
            this.Model.RunConfigurationMonkey = runConfiguration.IsChecked ?? false;
        }

        private void RunInfrastructure_Checked(object sender, RoutedEventArgs e)
        {
            this.Model.RunInfrastructureMonkey = runInfrastructure.IsChecked ?? false;
        }

        private void RunLibrary_Checked(object sender, RoutedEventArgs e)
        {
            this.Model.RunLibraryMonkey = runLibrary.IsChecked ?? false;
        }

        private void RunVariables_Checked(object sender, RoutedEventArgs e)
        {
            this.Model.RunVariableMonkey = runVariables.IsChecked ?? false;
        }

        private void RunRunbooks_Checked(object sender, RoutedEventArgs e)
        {
            this.Model.RunRunbookMonkey = runRunbooks.IsChecked ?? false;
        }

        #endregion

        #region Helpers

        public class SeaMonkeyProps
        {
            public string ServerUrl { get; set; }
            public string ServerApiKey { get; set; }

            public bool RunSetupMonkey { get; set; }
            public bool RunTenantMonkey { get; set; }
            public bool RunDeployMonkey { get; set; }
            public bool RunConfigurationMonkey { get; set; }
            public bool RunInfrastructureMonkey { get; set; }
            public bool RunLibraryMonkey { get; set; }
            public bool RunVariableMonkey { get; set; }
            public bool RunRunbookMonkey { get; set; }
        }

        private static string GetSeaMonkeyConsoleExePath()
        {
            // TODO: Hacky for now, but does the job.
            return "../../../Console/bin/Debug";

            //var assembly = // System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().GetName().CodeBase);
            //if (assembly == null) {
            //    return null;
            //}
            //// Strip file:// from start.
            //assembly = assembly.Substring(6);
            //return assembly;
        }

        #endregion

    }
}
