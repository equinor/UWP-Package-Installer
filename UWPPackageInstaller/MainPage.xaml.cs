using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Windows.ApplicationModel.Core;
using Windows.Foundation;
using Windows.Management.Deployment;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace UWPPackageInstaller
{
    // ReSharper disable once RedundantExtendsListEntry
    public sealed partial class MainPage : Page
    {
        readonly PackageManager _pkgManager = new PackageManager();

        public MainPage()
        {
            InitializeComponent();
            ApplicationView.PreferredLaunchViewSize = new Size(700, 500);
            ApplicationView.PreferredLaunchWindowingMode = ApplicationViewWindowingMode.PreferredLaunchViewSize;
        }

        private (Uri, string) _appDownloadInfo;

        public bool IsUrlValidForEcho(Uri fileUri)
        {
            // WARNING: This is a potential security issue: if anyone uses this URI they will be able to install apps on our HoloLenses.
            var hostAllowList = new List<string>
            {
                @"stemrappsdev.blob.core.windows.net",
                @"stemrappsprod.blob.core.windows.net"
            };

            return (hostAllowList.Contains(fileUri.Host) && fileUri.Scheme == "https");
        }


        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            if (e.Parameter is Uri uriParam)
            {
                var inputSasUrl = new Uri(uriParam.OriginalString.Remove(0, "echoinstaller://".Length));

                if (!IsUrlValidForEcho(inputSasUrl))
                {
                    PermissionTextBlock.Text = "Input url is invalid. Cannot install this app.";
                    return;
                }

                var filename = inputSasUrl.LocalPath.Split("/").Last();

                _appDownloadInfo = (inputSasUrl, filename);
                PermissionTextBlock.Text = $"Install {filename}?";
                InstallProgressBar.Visibility = Visibility.Collapsed;
                InstallValueTextBlock.Visibility = Visibility.Collapsed;
                InstallButton.Visibility = Visibility.Visible;
                CancelButton.Content = "Exit";
                PackageNameTextBlock.Text = filename;
            }
            else
            {
                // TODO: Actually create a link here, or open it directly
                PermissionTextBlock.Text = "Open echolens.equinor.com to install apps.";
                InstallButton.Visibility = Visibility.Collapsed;
            }
        }

        private void cancelButton_Click(object sender, RoutedEventArgs e)
        {
            CoreApplication.Exit();
        }

        private void installButton_Click(object sender, RoutedEventArgs e)
        {
            InstallButton.Visibility = Visibility.Collapsed;
            CancelButton.Visibility = Visibility.Collapsed;

            var (url, filename) = _appDownloadInfo;
            downloadAndInstall(url, filename);
        }

        private async void downloadAndInstall(Uri fileToDownload, string packageName)
        {
            InstallProgressBar.Visibility = Visibility.Visible;
            InstallValueTextBlock.Visibility = Visibility.Visible;

            Progress<DeploymentProgress> progressCallback = new Progress<DeploymentProgress>(installProgress);
            string resultText = "Nothing";

            Notification.ShowInstallationHasStarted(packageName);
            var pkgRegistered = true;

            try
            {
                var result = await _pkgManager.AddPackageAsync(fileToDownload, null,
                        DeploymentOptions.ForceApplicationShutdown | DeploymentOptions.ForceUpdateFromAnyVersion)
                    .AsTask(progressCallback);
                ensureIsAppRegistered(result);
            }

            catch (Exception e)
            {
                Debug.WriteLine(e);
                Debug.WriteLine(e.StackTrace);
                resultText = e.Message;
                pkgRegistered = false;
            }

            CancelButton.Content = "Exit";
            CancelButton.Visibility = Visibility.Visible;
            if (pkgRegistered)
            {
                PermissionTextBlock.Text = "Completed";
                Notification.ShowInstallationHasCompleted(packageName);
            }
            else
            {
                ResultTextBlock.Text = resultText;
                Notification.SendError(resultText);
            }
        }

        /// <summary>
        /// Check if the result was successful. If not registered, throws Exception!
        /// </summary>
        /// <param name="result"></param>
        private static void ensureIsAppRegistered(DeploymentResult result)
        {
            if (result.IsRegistered) 
                return;
            
            Debug.WriteLine(result.ErrorText);
            throw result.ExtendedErrorCode;
        }

        /// <summary>
        /// Updates the progress bar and status of the installation in the app UI.
        /// </summary>
        /// <param name="installProgress"></param>
        private void installProgress(DeploymentProgress installProgress)
        {
            double installPercentage = installProgress.percentage;
            PermissionTextBlock.Text = "Installing...";
            InstallProgressBar.Value = installPercentage;
            var displayText = string.Format($"{installPercentage}%");
            InstallValueTextBlock.Text = displayText;
        }
    }
}