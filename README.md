# Echo App Installer

Public Repository.

Internal Runbook: <https://github.com/equinor/Echo/docs/emr-echolens-apps-installer.md>

## What is it?

An UWP installer for remote packages. 

The App installs the packages, displays errors in case something goes wrong.

The intended use of this is to create an alternative app-installer for the HoloLens 2 the built in app installer on Windows 10 and Windows Device Portal (for sideloading apps onto your device).

## Known Issues
    
HoloLens 1 is not supported. 

# Publishing

The app is published as a LOB app to the Equinor App Store.

To publish you need access to the App Store.

Publising should currently be done with Visual Studio "Publish" dialogue, and manually uploaded to <https://partner.microsoft.com> Equinor ASA organization.

## Credits

This codebase is forked from @colinkiama's UWP-Package-Installer: <https://github.com/colinkiama/UWP-Package-Installer>.
