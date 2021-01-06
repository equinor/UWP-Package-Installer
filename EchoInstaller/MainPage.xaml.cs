using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.ApplicationModel.ExtendedExecution;
using Windows.Foundation;
using Windows.Management.Deployment;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Documents;
using Windows.UI.Xaml.Navigation;
using EchoInstaller.Validators;
using Microsoft.Toolkit.Uwp.Helpers;

namespace EchoInstaller
{
    // ReSharper disable once RedundantExtendsListEntry
    public sealed partial class MainPage : Page
    {
        private const string AppUri = "echoinstaller://";

        private readonly PackageManager _packageManager = new PackageManager();

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

            var sysInfoDisplayText = getSystemInformationText();
            AboutApp.Text = sysInfoDisplayText;

            if (e.Parameter is Uri uriParam)
            {
                var inputSasUrl = new Uri(uriParam.OriginalString.Remove(0, AppUri.Length));

                if (!EchoUrlValidator.IsUrlValidForEcho(inputSasUrl))
                {
                    PermissionTextBlock.Text = "Input url is invalid. Cannot install this app.\nUrl is: " + inputSasUrl;
                    _appDownloadInfo = (null, null);
                    return;
                }

                var filename = inputSasUrl.LocalPath.Split("/").Last();

                _appDownloadInfo = (inputSasUrl, filename);
                PermissionTextBlock.Text = $"Install {filename}?";
                InstallValueTextBlock.Visibility = Visibility.Visible;
                InstallValueTextBlock.Text = $"Note: There is a known issue with the Echo Installer. For some users the Installation Progress seemingly stops, but the app continues installing in the background. Check in the Start menu if the app is installed successfully after a while.\n\nPress Install to start installing {filename}.";
                InstallProgressBar.Visibility = Visibility.Collapsed;
                InstallButton.Visibility = Visibility.Visible;
                CancelButton.Content = "Exit";
                PackageNameTextBlock.Text = filename;
            }
            else
            {
                // If app is not started with an URI we just redirect to the website in the browser.
                // We could consider embedding a webview here instead.
                var hyperlink = new Hyperlink()
                {
                    NavigateUri = new Uri("https://echolens.equinor.com")
                };
                hyperlink.Inlines.Add(new Run() { Text = "https://echolens.equinor.com" });

                PermissionTextBlock.Inlines.Clear();
                var firstLine = new Run { Text = "Open " };
                PermissionTextBlock.Inlines.Add(firstLine);
                PermissionTextBlock.Inlines.Add(hyperlink);
                var lastLine = new Run() { Text = " to browse and install available apps." };
                PermissionTextBlock.Inlines.Add(lastLine);

                InstallButton.Visibility = Visibility.Collapsed;
            }
        }


        private static string getSystemInformationText()
        {
            var appName = SystemInformation.ApplicationName;
            var appVersion = SystemInformation.ApplicationVersion;
            var culture = SystemInformation.Culture;
            var os = SystemInformation.OperatingSystem;
            var processorArchitecture = SystemInformation.OperatingSystemArchitecture;
            var osVersion = SystemInformation.OperatingSystemVersion;
            var deviceFamily = SystemInformation.DeviceFamily;
            var deviceModel = SystemInformation.DeviceModel;
            var deviceManufacturer = SystemInformation.DeviceManufacturer;

            var text =
                $"{appName}:{appVersion.ToFormattedString()}\n{os} {osVersion} on {processorArchitecture}\n{deviceFamily}: {deviceModel} {deviceManufacturer}\n{culture}";

            return text;
        }

        private void cancelButton_Click(object sender, RoutedEventArgs e)
        {
            CoreApplication.Exit();
        }

        private async void installButton_Click(object sender, RoutedEventArgs e)
        {
            InstallButton.Visibility = Visibility.Collapsed;
            CancelButton.Visibility = Visibility.Collapsed;

            var (url, filename) = _appDownloadInfo;

            using (var session = new ExtendedExecutionSession())
            {
                session.Reason = ExtendedExecutionReason.Unspecified;
                session.Description = "Installing App to Device";
                session.Revoked += (_, args) => Trace.WriteLine("Extended Session Revoked: " + args.Reason);
                ExtendedExecutionResult extendedExecutionResult = await session.RequestExtensionAsync();

                Trace.WriteLine(extendedExecutionResult);

                await downloadAndInstall(url, filename);
            }
        }

        private async Task downloadAndInstall(Uri fileToDownload, string packageName)
        {

            var resultText = "Nothing";
            var pkgRegistered = true;



            InstallProgressBar.Visibility = Visibility.Visible;
            InstallValueTextBlock.Visibility = Visibility.Visible;

            IProgress<DeploymentProgress> progressCallback = new Progress<DeploymentProgress>(installProgress);

            Notification.ShowInstallationHasStarted(packageName);

            try
            {
                var sw = Stopwatch.StartNew();
                var addPackageOperation = _packageManager.AddPackageAsync(fileToDownload, null,
                    DeploymentOptions.ForceApplicationShutdown | DeploymentOptions.ForceUpdateFromAnyVersion);

                // There is no progress callback while downloading (that I have found)
                PermissionTextBlock.Text = "Downloading 📥";
                InstallValueTextBlock.Text = "Progress will remain at 0% until download is complete.";

                // Subscribe to the progress callback.
                addPackageOperation.Progress += (_, progressInfo) => { progressCallback.Report(progressInfo); };

                //if (addPackageOperation.Status != AsyncStatus.Completed)
                //{
                //    Console.WriteLine(
                //        $"Add Package Status: {addPackageOperation.Status}. Exception: {addPackageOperation.ErrorCode}");
                //}

                Trace.WriteLine($"Add Package Started");

                // For some reason `await addPackageOperation` stops silently after ~30 seconds when building Release builds.
                // (The task still runs, but no progress is ever reported to the EchoInstaller. The apps usually install successfully.)
                // Tested variants that stops silently
                //     - await addPackageOperation.AsTask(progressCallback);
                //     - await addPackageOperation;
                // Instead of awaiting we do a hacky "while" loop. This seems to solve the issue.
                var delay = TimeSpan.FromSeconds(0.2);
                while (addPackageOperation.Status == AsyncStatus.Started)
                    await Task.Delay(delay);

                var result = addPackageOperation.GetResults();
                Trace.WriteLine($"Add Package Finished after " + sw.Elapsed);
                
                ensureIsAppRegistered(result);
            }

            catch (Exception e)
            {
                Trace.WriteLine(e);
                Trace.WriteLine(e.StackTrace);
                resultText = e.Message;
                pkgRegistered = false;
            }


            CancelButton.Content = "Exit";
            CancelButton.Visibility = Visibility.Visible;
            if (pkgRegistered)
            {
                PermissionTextBlock.Text = "Installation Complete ✔";
                ResultTextBlock.Text = @"App installed successfully!

The Installed App can be found in the Start menu under ""All Apps"".

You can now close this window by clicking X in the top right corner.";
                Notification.ShowInstallationHasCompleted(packageName);
            }
            else
            {
                ResultTextBlock.Text = "Error: " + resultText + " ❌";
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

            Trace.WriteLine(result.ErrorText);
            throw result.ExtendedErrorCode;
        }




        /// <summary>
        /// Updates the progress bar and status of the installation in the app UI.
        /// </summary>
        /// <param name="installProgress"></param>
        private void installProgress(DeploymentProgress installProgress)
        {
            switch (installProgress.state)
            {
                case DeploymentProgressState.Queued:
                    PermissionTextBlock.Text = "Queued...";
                    break;
                case DeploymentProgressState.Processing:
                    double installPercentage = installProgress.percentage;
                    PermissionTextBlock.Text = "Installing 🏗";
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