using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.Background;
using Windows.Management.Deployment;

namespace InstallTask
{
    public sealed class Install : IBackgroundTask
    {
        BackgroundTaskDeferral _deferral;
        string _resultText = "Nothing";
        bool _pkgRegistered = false;
        private static IBackgroundTaskInstance _boom;
        static double _installPercentage = 0;
        /// <summary>
        /// Pretty much identical to showProgressInApp() in MainPage.xaml.cs
        /// </summary>
        /// <param name="taskInstance"></param>
        public async void Run(IBackgroundTaskInstance taskInstance)
        {
            _boom = taskInstance;
            _deferral = taskInstance.GetDeferral();
            ApplicationTriggerDetails details = (ApplicationTriggerDetails)taskInstance.TriggerDetails;
            string packagePath = "";

            packagePath = (string)details.Arguments["packagePath"];
            PackageManager pkgManager = new PackageManager();
            Progress<DeploymentProgress> progressCallback = new Progress<DeploymentProgress>(installProgress);
            Notification.SendUpdatableToastWithProgress(0);
            if ((int)details.Arguments["installType"] == 1)
            {
                List<Uri> dependencies = new List<Uri>();
                var dependencyPairs = details.Arguments.Where(p => p.Key.Contains("d")).ToList();
                foreach (var dependencyPair in dependencyPairs)
                {
                    string dependencyAsString = (string)dependencyPair.Value;
                    dependencies.Add(new Uri(dependencyAsString));
                    
                }

                try
                {
                    var result = await pkgManager.AddPackageAsync(new Uri(packagePath), dependencies, DeploymentOptions.ForceTargetApplicationShutdown).AsTask(progressCallback);
                    checkIfPackageRegistered(result, _resultText);
                }

                catch (Exception e)
                {
                    _resultText = e.Message;
                }

            }
            else
            {
                try
                {
                    var result = await pkgManager.AddPackageAsync(new Uri(packagePath), null, DeploymentOptions.ForceTargetApplicationShutdown).AsTask(progressCallback);
                    checkIfPackageRegistered(result, _resultText);
                }

                catch (Exception e)
                {
                    _resultText = e.Message;
                }


            }


            if (_pkgRegistered == true)
            {
                Notification.ShowInstallationHasCompleted();
            }
            else
            {
                Notification.ShowError(_resultText);
            }


            _deferral.Complete();
        }


        private static void installProgress(DeploymentProgress installProgress)
        {
             _installPercentage = installProgress.percentage;
            _boom.Progress = (uint)_installPercentage;
            Notification.UpdateProgress(_installPercentage);
        }




        private void checkIfPackageRegistered(DeploymentResult result, string resultText)
        {
            if (result.IsRegistered)
            {
                _pkgRegistered = true;
            }
            else
            {
                resultText = result.ErrorText;
            }
        }




    }
}
