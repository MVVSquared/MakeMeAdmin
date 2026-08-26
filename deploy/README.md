# Deployment

Make Me Admin can be deployed from Intune as a **line-of-business MSI**. Starting with 2.3.83, policy settings and the web log API key can be passed as MSI properties so you do not need a second Intune script for those values.

## MSI Builder (configured installer)

`MakeMeAdminMsiBuilder.exe` is a standalone app. Run it, fill in the settings you want (including an optional API key), and it writes a copy of the x64 MSI with those values already in the Property table. Upload that MSI to Intune with **no command-line arguments**.

Build **Setup** (Release | x64) first so the template includes the policy registry components, then build **MsiBuilder**. The builder exe is copied to `Installers\MakeMeAdminMsiBuilder.exe`.

Leave a field blank (or on “software default”) to omit that setting; the installed product then uses its built-in default.

The API key is **not** stored as plaintext in the registry. The installer (running as LocalSystem) DPAPI-protects it and writes `%ProgramData%\Make Me Admin\weblog-apikey.bin`. The service unprotects that blob when it calls the web log endpoint.

## Intune (preferred): one MSI

1. Upload the x64 (or x86) MSI as a Line-of-business app.
2. Set **Command-line arguments** to MSI properties (no `msiexec` prefix). Example:

```
WEBLOGENDPOINT="https://logs.example.wsu.edu/ingest" WEBLOGAPIKEY="your-api-key-here" PROMPTFORREASON=2 REMOVEADMINRIGHTSONLOGOUT=1
```

3. Assign the app. Updates are a newer MSI with the same UpgradeCode; keep the same command-line arguments so the key is rewritten on upgrade.

| Property | Registry / storage | Notes |
|----------|--------------------|--------|
| **WEBLOGENDPOINT** | `WebLogEndpoint` (`REG_SZ`) | HTTPS URL for log POST. Omit to disable web logging. |
| **WEBLOGAPIKEY** | DPAPI blob under ProgramData | Hidden/secure MSI property. Do not put this in Settings Catalog. |
| **PROMPTFORREASON** | `Prompt For Reason` (`REG_DWORD`) | 0 = None, 1 = Optional, 2 = Required. |
| **REMOVEADMINRIGHTSONLOGOUT** | `Remove Admin Rights On Logout` (`REG_DWORD`) | 0 or 1. |

The key still exists in the Intune command-line field (Intune admins can see it) and inside the cached MSI property table. After install, on the device, standard users cannot decrypt the blob.

## Existing script (still supported)

`MakeMeAdmin_Working.ps1` can still write a plaintext `WebLogApiKey` registry value. On service start (and on the next web log send), that value is migrated into the DPAPI blob and deleted from the registry. Prefer the MSI property for new assignments.

Policy key used by the script:

`HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Sinclair Community College\Make Me Admin`

### Settings in This Script

| Setting | Type | Values | Description |
|---------|------|--------|-------------|
| **WebLogEndpoint** | `REG_SZ` | URL string | The URL of a web endpoint to receive log entries. Leave empty or omit to disable web logging. |
| **WebLogApiKey** | `REG_SZ` | API key string | Optional. Migrated to a DPAPI blob; do not rely on this remaining in the registry. Prefer `WEBLOGAPIKEY` on the MSI. |
| **Prompt For Reason** | `REG_DWORD` | 0, 1, or 2 | Whether to prompt the user for a reason when requesting administrator rights. **0** = None, **1** = Optional, **2** = Required. |
| **Remove Admin Rights On Logout** | `REG_DWORD` | 0 or 1 | When **1**, administrator rights are revoked when the user logs off. **0** = disabled (default). |
| **Remove Admin Rights On Disconnect** | `REG_DWORD` | 0 or 1 | When **1**, administrator rights are revoked when the user disconnects from a remote session (e.g., RDP disconnect). **0** = disabled. |
| **Remove Admin Rights On Lock** | `REG_DWORD` | 0 or 1 | When **1**, administrator rights are revoked when the user locks the workstation. **0** = disabled. |
| **Remove Admin Rights On Sleep** | `REG_DWORD` | 0 or 1 | When **1**, administrator rights are revoked when the computer goes to sleep. **0** = disabled. |
| **Remove Admin Rights On Screen Saver** | `REG_DWORD` | 0 or 1 | When **1**, administrator rights are revoked when the screen saver activates. **0** = disabled. |

### Other Common Settings (Not in Script)

You can add these to the script if needed:

| Setting | Type | Description |
|---------|------|-------------|
| **Allowed Entities** | `REG_MULTI_SZ` | SIDs or names (e.g., `DOMAIN\GroupName`) of users/groups allowed to obtain admin rights. |
| **Denied Entities** | `REG_MULTI_SZ` | SIDs or names of users/groups denied admin rights (takes precedence over Allowed). |
| **Admin Rights Timeout** | `REG_DWORD` | Default minutes the user remains in the Administrators group (default: 10). |
| **Renewals Allowed** | `REG_DWORD` | Number of times a user can renew their admin rights (default: 0). |

For the complete list of upstream MakeMeAdmin settings, see the [official registry settings documentation](https://github.com/pseymour/MakeMeAdmin/wiki/Registry-Settings).
