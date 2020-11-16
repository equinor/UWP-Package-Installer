using System;
using NUnit.Framework;
using UWPPackageInstaller;

namespace UWPPackageInstallerTests
{
    [TestFixture]
    public class EchoUrlValidatorTests
    {
        [TestCase("http://echolens.equinor.com", ExpectedResult = false)]
        [TestCase("http://echolens.equinor.com", ExpectedResult = false)]
        [TestCase("https://echo.equinor.com", ExpectedResult = false)]
        [TestCase("https://stemrappsdev.blob.core.windows.net", ExpectedResult = true)]
        [TestCase("http://stemrappsdev.blob.core.windows.net", ExpectedResult = true)]
        [TestCase("https://stemrappsprod.blob.core.windows.net", ExpectedResult = true)]
        [TestCase("http://stemrappsprod.blob.core.windows.net", ExpectedResult = true)]
        [TestCase("https://stemrappsprod.blob.core.windows.net?url=test", ExpectedResult = true)]
        public bool IsUrlValidForEcho_OnlyWhenUrlIsWhitelistedAndHttps_ReturnsTrue(string url)
        {
            var testUri = new Uri(url);
            return EchoUrlValidator.IsUrlValidForEcho(testUri);
        }
    }
}