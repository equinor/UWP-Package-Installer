using System;
using System.Collections.Generic;

namespace UWPPackageInstaller
{
    public sealed class EchoUrlValidator
    {
        public static bool IsUrlValidForEcho(Uri fileUri)
        {
            // WARNING: This is a potential security issue: if anyone hijacks these URIs they will be able to install apps on our HoloLenses.
            var hostAllowList = new List<string>
            {
                @"stemrappsdev.blob.core.windows.net",
                @"stemrappsprod.blob.core.windows.net"
            };

            return (hostAllowList.Contains(fileUri.Host) && fileUri.Scheme == "https");
        }
    }
}