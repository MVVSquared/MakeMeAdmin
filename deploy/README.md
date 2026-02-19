# Deployment Scripts

These scripts are **not part of the MakeMeAdmin build**. They are add-on utilities used to configure MakeMeAdmin after installation—for example, setting registry values for Intune/MDM deployment.

## Registry Settings Reference

The `MakeMeAdmin_Working.ps1` script configures MakeMeAdmin via the policy registry key:

`HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Sinclair Community College\Make Me Admin`

Below is a reference for the registry settings used in the script. For the complete list of all MakeMeAdmin settings, see the [official MakeMeAdmin Registry Settings documentation](https://github.com/pseymour/MakeMeAdmin/wiki/Registry-Settings).

### Settings in This Script

| Setting | Type | Values | Description |
|---------|------|--------|-------------|
| **WebLogEndpoint** | `REG_SZ` | URL string | *(Extension)* The URL of a web endpoint to receive log entries. When configured, MakeMeAdmin sends log events (message, eventId, severity) via HTTP POST. Leave empty or omit to disable web logging. |
| **WebLogApiKey** | `REG_SZ` | API key string | *(Extension)* Optional. When set, sent as the `X-Api-Key` header for authenticated requests to the WebLogEndpoint. Use Intune or another secure method to deploy this value—do not store secrets in scripts. |
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
