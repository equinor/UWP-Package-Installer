using Windows.UI.Notifications;
using Microsoft.Toolkit.Uwp.Notifications;

namespace EchoInstaller
{
    public class Notification
    {
        public static void ShowInstallationHasStarted(string packageName)
        {
            showToast("Install Status", $"{packageName} is installing...");
        }

        public static void SendError(string errorText)
        {
            showToast("Install Status", "Installation has failed");
        }

        public static void ShowInstallationHasCompleted(string packageName)
        {
            showToast("Install Status", $"Installation of {packageName} Is Complete!");
        }

        private static void showToast(string header, string contentText)
        {
            // Define a tag value and a group value to uniquely identify a notification, in order to target it to apply the update later;
            var toastTag = "echoAppInstall";
            var toastGroup = "eaiGroup";

            var content = new ToastContent()
            {
                Visual = new ToastVisual()
                {
                    BindingGeneric = new ToastBindingGeneric()
                    {
                        Children =
                        {
                            new AdaptiveText()
                            {
                                Text = header
                            },

                            new AdaptiveText()
                            {
                                Text = contentText
                            }
                        }
                    }
                }
            };

            // Generate the toast notification;
            var toast = new ToastNotification(content.GetXml()) { Tag = toastTag, Group = toastGroup };

            // Show the toast notification to the user;
            ToastNotificationManager.CreateToastNotifier().Show(toast);
        }

    }
}