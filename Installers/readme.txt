================================================================================
Make Me Admin - Registry Configuration Guide
================================================================================

Make Me Admin is configured through the Windows registry. Settings control who
may request administrator rights, how long rights last, logging destinations,
and related behavior.


--------------------------------------------------------------------------------
REGISTRY PATHS
--------------------------------------------------------------------------------

Preference (local / non-enforced) settings:

  HKEY_LOCAL_MACHINE\SOFTWARE\Sinclair Community College\Make Me Admin

Policy (enforced) settings — recommended for Intune, GPO, or MDM:

  HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Sinclair Community College\Make Me Admin

Precedence:
  - If a value exists under the Policies key, it is used.
  - Otherwise the preference key is used.
  - If neither is set, the application default applies.

Create the key if it does not exist before adding values.


--------------------------------------------------------------------------------
HOW TO SET VALUES
--------------------------------------------------------------------------------

Using Registry Editor (regedit.exe):
  1. Open regedit as an administrator.
  2. Navigate to (or create) one of the keys above.
  3. Create a new value with the exact name listed below.
  4. Set the type (DWORD, String, or Multi-String) and the value.

Using PowerShell (run elevated) — example for the Policies key:

  $path = "HKLM:\SOFTWARE\Policies\Sinclair Community College\Make Me Admin"
  if (!(Test-Path $path)) { New-Item -Path $path -Force | Out-Null }

  # DWORD example
  Set-ItemProperty -Path $path -Name "Admin Rights Timeout" -Value 15 -Type DWord

  # String example
  Set-ItemProperty -Path $path -Name "WebLogEndpoint" -Value "https://logs.example.com/api" -Type String

  # Multi-String example
  New-ItemProperty -Path $path -Name "Allowed Entities" -PropertyType MultiString `
    -Value @("DOMAIN\HelpDesk","S-1-5-21-...") -Force


--------------------------------------------------------------------------------
SETTING REFERENCE
--------------------------------------------------------------------------------

Name                                 Type            Default     Description
--------------------------------------------------------------------------------

Allowed Entities                     REG_MULTI_SZ    (empty)     Users/groups (SID or
                                                                 DOMAIN\Name) allowed to
                                                                 obtain admin rights.

Denied Entities                      REG_MULTI_SZ    (empty)     Users/groups denied admin
                                                                 rights. Denial wins over
                                                                 Allowed Entities.

Automatic Add Allowed                REG_MULTI_SZ    (empty)     Users/groups added to
                                                                 Administrators automatically
                                                                 at logon.

Automatic Add Denied                 REG_MULTI_SZ    (empty)     Users/groups never auto-
                                                                 added. Denial wins over
                                                                 Automatic Add Allowed.

Remote Allowed Entities              REG_MULTI_SZ    (empty)     Users/groups allowed to
                                                                 request rights remotely.

Remote Denied Entities               REG_MULTI_SZ    (empty)     Users/groups denied remote
                                                                 requests. Denial wins.

Admin Rights Timeout                 REG_DWORD       10          Minutes the user remains
                                                                 in the Administrators group.

Renewals Allowed                     REG_DWORD       0           How many times a user may
                                                                 renew their admin rights.

Timeout Overrides                    (subkey)        (none)      Subkey under the settings
                                                                 key. Each REG_SZ value name
                                                                 is a SID or DOMAIN\Name;
                                                                 the value is timeout minutes.
                                                                 Highest matching timeout
                                                                 for a user wins.

Remove Admin Rights On Logout        REG_DWORD       0           0 = keep rights after logoff
                                                                 until timeout (default)
                                                                 1 = remove rights on logoff

Override Removal By Outside Process  REG_DWORD       0           0 = do not re-add (default)
                                                                 1 = re-add the user if another
                                                                 process (e.g. GPO) removes them

Require Authentication For Privileges REG_DWORD      0           0 = disabled (default)
                                                                 1 = require authentication
                                                                 before granting privileges

Allow Remote Requests                REG_DWORD       0           0 = disabled (default)
                                                                 1 = allow remote requests

End Remote Sessions Upon Expiration  REG_DWORD       1           0 = leave remote session open
                                                                 1 = end remote session when
                                                                 rights expire (default)

Close Application Upon Expiration    REG_DWORD       0           0 = leave UI open (default)
                                                                 1 = close Make Me Admin UI
                                                                 when rights expire

Log Off After Expiration             REG_DWORD       0           Minutes after expiration to
                                                                 force logoff (0 = disabled)

Log Off Message                      REG_MULTI_SZ    (empty)     Message lines shown before
                                                                 forced logoff.

Prompt For Reason                    REG_DWORD       0           0 = None (no prompt)
                                                                 1 = Optional
                                                                 2 = Required

Allow Free-Form Reason               REG_DWORD       1           0 = canned reasons only
                                                                 1 = allow free-text reason
                                                                 (default)

Canned Reasons                       REG_MULTI_SZ    (empty)     Predefined reason strings
                                                                 offered in the prompt UI.

Maximum Reason Length                REG_DWORD       333         Max characters for a reason.

Log Elevated Processes               REG_DWORD       0           0 = Never (default)
                                                                 1 = Only when user is admin
                                                                 2 = Always

TCP Service Port                     REG_DWORD       808         Port used by the Make Me
                                                                 Admin service for remote
                                                                 communication.

syslog servers                       REG_MULTI_SZ    (empty)     One or more syslog targets.
                                                                 See SYSLOG FORMAT below.


--------------------------------------------------------------------------------
WEB LOGGING (EXTENSION IN THIS BUILD)
--------------------------------------------------------------------------------

These values are read from the Policies key only:

  HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Sinclair Community College\Make Me Admin

Name                                 Type            Default     Description
--------------------------------------------------------------------------------

WebLogEndpoint                       REG_SZ          (none)      HTTPS/HTTP URL that receives
                                                                 log events via HTTP POST.
                                                                 Omit or leave empty to
                                                                 disable web logging.

WebLogApiKey                         REG_SZ          (none)      Optional API key sent as the
                                                                 X-Api-Key request header.
                                                                 Prefer secure deployment
                                                                 (Intune/secrets); do not
                                                                 hard-code secrets in scripts.

When WebLogEndpoint is set, Make Me Admin POSTs JSON of the form:

  {"message": "...", "eventId": "...", "severity": "..."}


--------------------------------------------------------------------------------
SYSLOG FORMAT
--------------------------------------------------------------------------------

Value name:  syslog servers
Value type:  REG_MULTI_SZ
Entry form:  server_address:port:protocol:RFC

  server_address  Hostname or IP of the syslog server
  port            Listening port (optional; defaults by protocol)
  protocol        tcp or udp (optional; default udp)
  RFC             3164 or 5424 (optional; default 3164)

Examples:

  syslogserver
  syslogserver:udp
  syslogserver:tcp
  syslogserver:514:udp
  syslogserver.domain.edu:514:udp
  syslogserver:1468:tcp
  syslogserver:1468:tcp:5424


--------------------------------------------------------------------------------
ENTITY NAME FORMAT
--------------------------------------------------------------------------------

Allowed / Denied / Automatic Add / Remote lists accept:

  - Windows SIDs, e.g.  S-1-5-21-3623811015-3361044348-30300820-1013
  - Account names, e.g. DOMAIN\UserName  or  DOMAIN\GroupName


--------------------------------------------------------------------------------
EXAMPLE: TYPICAL INTUNE / LAB CONFIGURATION
--------------------------------------------------------------------------------

Path:
  HKLM\SOFTWARE\Policies\Sinclair Community College\Make Me Admin

  Allowed Entities              MULTI_SZ   DOMAIN\StudentsWhoNeedAdmin
  Admin Rights Timeout          DWORD      15
  Prompt For Reason             DWORD      2          (Required)
  Remove Admin Rights On Logout DWORD      1
  WebLogEndpoint                SZ         https://your-log-server.example/api/logs
  WebLogApiKey                  SZ         (deploy via secure method)


--------------------------------------------------------------------------------
NOTES
--------------------------------------------------------------------------------

- Value names are case-sensitive as documented above; use the exact spelling.
- Boolean DWORD settings: 0 = false/disabled, 1 = true/enabled.
- After changing policy registry values, a service restart may be required for
  some settings to take effect (Services: Make Me Admin).
- Group Policy ADMX/ADML templates ship with the product installation and can
  manage most native settings. WebLogEndpoint / WebLogApiKey are extensions
  and are set via registry (or your MDM registry payload), not via ADMX.

================================================================================
