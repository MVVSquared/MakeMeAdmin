Write-Host "Starting MakeMeAdmin configuration script"

try {
    $registryPath = "HKLM:\SOFTWARE\Policies\Sinclair Community College\Make Me Admin"
    Write-Host "Registry path: $registryPath"

    if (!(Test-Path $registryPath)) {
        New-Item -Path $registryPath -Force | Out-Null
        Write-Host "Created registry path"
    } else {
        Write-Host "Registry path already exists"
    }

    # Define the registry values
    $registryValues = @{
        "WebLogEndpoint" = "your-web-log-endpoint-here"
        "Prompt For Reason" = 2
        "WebLogApiKey" = "your-api-key-here"
        "Remove Admin Rights On Logout" = 1
        "Remove Admin Rights On Disconnect" = 1
        "Remove Admin Rights On Lock" = 1
        "Remove Admin Rights On Sleep" = 1
        "Remove Admin Rights On Screen Saver" = 1
    }

    Write-Host "Registry values defined"

    # Set each registry value
    $successCount = 0
    foreach ($key in $registryValues.Keys) {
        $value = $registryValues[$key]
        if ($value -is [int]) {
            Set-ItemProperty -Path $registryPath -Name $key -Value $value -Type DWord
            Write-Host "Set DWORD: $key = $value"
        } else {
            Set-ItemProperty -Path $registryPath -Name $key -Value $value -Type String
            Write-Host "Set String: $key = $value"
        }
        $successCount++
    }

    Write-Host "Successfully set $successCount registry values"
    Write-Host "Script completed successfully"
}
catch {
    Write-Host "Script failed with error: $($_.Exception.Message)"
}
