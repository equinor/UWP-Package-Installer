using System;
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
        readonly PackageManager _packageManager = new PackageManager();

        public MainPage()
        {
            InitializeComponent();
            ApplicationView.PreferredLaunchViewSize = new Size(700, 500);
            ApplicationView.PreferredLaunchWindowingMode = ApplicationViewWindowingMode.PreferredLaunchViewSize;
        }

        private (Uri, string) _appDownloadInfo;

       protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            if (e.Parameter is Uri uriParam)
            {
                var inputSasUrl = new Uri(uriParam.OriginalString.Remove(0, "echoinstaller://".Length));

                if (!EchoUrlValidator.IsUrlValidForEcho(inputSasUrl))
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

            IProgress<DeploymentProgress> progressCallback = new Progress<DeploymentProgress>(installProgress);
            string resultText = "Nothing";

            Notification.ShowInstallationHasStarted(packageName);
            var pkgRegistered = true;

            try
            {
                var addPackageOperation = _packageManager.AddPackageAsync(fileToDownload, null,
                    DeploymentOptions.ForceApplicationShutdown | DeploymentOptions.ForceUpdateFromAnyVersion);
                // There is no progress callback while downloading (that I have found)
                PermissionTextBlock.Text = "Downloading files... Download progress is unknown.";

                // Subscribe to the progress callback.
                addPackageOperation.Progress += (_, progressInfo) =>
                {
                    progressCallback.Report(progressInfo);
                };
                
                var result = await addPackageOperation;

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
            switch (installProgress.state){
                case DeploymentProgressState.Queued:
                    PermissionTextBlock.Text = "Queued...";
                    break;
                case DeploymentProgressState.Processing:
                    double installPercentage = installProgress.percentage;
                    PermissionTextBlock.Text = "Installing...";
                    InstallProgressBar.Value = installPercentage;
                    var displayText = string.Format($"{installPercentage}%");
                    InstallValueTextBlock.Text = displayText;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}