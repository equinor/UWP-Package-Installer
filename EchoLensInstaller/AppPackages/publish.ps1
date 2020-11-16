<#
.SYNOPSIS
    Upload the build artifacts to Azure Blob Storage.
    This blob storage must be the same as defined in the publish step.
.DESCRIPTION
    
.EXAMPLE
    PS C:\> publish.ps1 -StorageAccount "staccount" -TargetContainer '$web' -Force
    Will upload all files in the publish folder recursively!
.INPUTS
    Inputs (if any)
.OUTPUTS
    Output (if any)
.NOTES
    General notes
#>
[CmdletBinding(
    SupportsShouldProcess,
    ConfirmImpact = 'High'
)]
param (
    [Parameter(Mandatory = $true)]
    [string]
    $StorageAccount = "",
    [Parameter(Mandatory = $false)]
    [string]
    $TargetContainer = '$web',
    [Parameter()]
    [switch]
    $Force
)

# Avoid Annoying errors
Set-StrictMode -Version 3.0
$ErrorActionPreference = 'Stop'

# Helper method to convert from Azure CLI
function ConvertFrom-AzureCli {
    [CmdletBinding()]
    param (
        [Parameter(ValueFromPipeline = $true)] [string] $line
    )
    
    begin {
        $line = ""
    }
    
    process {
        $lines += $line + "`n"
    }
    
    end {
        [console]::ResetColor();
        $exitCode = $LASTEXITCODE
        if ($exitCode) {
            Write-Error "az cli exited with exit code $exitCode" -ErrorAction Stop
        }

        return ConvertFrom-Json -InputObject $lines
    }
}

# Get a connection string from the storage account.
$storageConnectionStringResult = (az storage account show-connection-string `
        --name $StorageAccount `
    | ConvertFrom-AzureCli)

$storageConnectionString = $storageConnectionStringResult.connectionString

$null = Get-ChildItem -Path "$PSScriptRoot" -Recurse | ForEach-Object { Write-Host "Will Upload: $_" }

Write-Host ""
if (-not ($Force -or $PSCmdlet.ShouldContinue("c", "Are you sure you want to publish all these files to production? (Publically open Storage!)"))) {
    Write-Host "Publish Cancelled."
    exit(0)
}

# Uploading this entire directory (!)
az storage blob upload-batch `
    --destination $TargetContainer `
    --source "$PSScriptRoot" `
    --account-name "$StorageAccount" `
    --connection-string "$storageConnectionString"
| ConvertFrom-AzureCli

# The appinstaller needs to have a special content type
$null = (az storage blob update `
        --container-name $TargetContainer `
        --name "EchoLensInstaller.appinstaller" `
        --content-type "application/appinstaller" `
        --connection-string "$storageConnectionString"
    | ConvertFrom-AzureCli)
    
Write-Host "All Done. Files published successfully!"
