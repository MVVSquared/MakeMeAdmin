Make Me Admin installer settings
================================

Use MakeMeAdminMsiBuilder.exe in this folder. Fill in only the settings you
want. Leave a field on "(software default)" or blank to keep the product
default. Save the MSI and upload it to Intune as a line-of-business app.
Command-line arguments are not required.

Build two MSIs if you need both of these:

  Individual (user-enrolled) PCs
    Check "Automatically allow the Entra / Intune user who enrolled this
    device." Optionally list IT staff in Allowed entities. The enrollee and
    anyone on that list can request admin. Anyone else cannot.

  Labs / shared PCs
    Leave that box unchecked. List lab or IT accounts in Allowed entities.


Logging
-------
Syslog servers              One per line: host, or host:port:protocol:RFC
                            Protocol is tcp or udp. RFC is 3164 or 5424.
                            Example: syslog.example.edu:514:udp:3164

Web log endpoint            HTTPS URL that receives JSON log POSTs.
                            Leave blank to disable web logging.

API key                     Optional. Stored in the MSI, then DPAPI-protected
                            on the PC at install. Not left in the registry.

Log elevated processes      Never (default), only when the user is an
                            administrator, or always.


Authorization
-------------
Allowed entities            Who may click Grant. One SID or name per line.
                            Examples: DOMAIN\HelpDesk
                                      AzureAD\user@wsu.edu
                                      S-1-5-32-544

Denied entities             Never allowed. Denied wins over Allowed and over
                            the enrolled-user checkbox.

Automatically allow the     The Entra/Intune user who enrolled this PC may
Entra / Intune user who     request admin, in addition to Allowed entities.
enrolled this device        Empty Allowed list + this box = enrollee only.

Automatically add at        Users/groups put in Administrators at logon
logon (allowed / denied)    without clicking Grant. Different from the
                            enrolled-user checkbox.


Session
-------
Admin rights timeout        Minutes in Administrators. Default 10.

Renewals allowed            Extra time-boxes the user may accept. Default 0.

Remove admin rights on      Yes = take rights away at logoff, not only when
logout                      the timer expires.

Close the application       Yes = close the UI when rights expire.
when rights expire

Put the user back in        Yes = re-add them if GPO or another process
Administrators if another   removed them before timeout.
process removes them

Log the user off after      Seconds after expiry to force logoff. 0 or blank
rights expire               = do not log off.

Log-off warning message     Shown before a forced logoff. One line per
                            paragraph.


Reason
------
Prompt for a reason         None (default), Optional, or Required.

Allow a free-form reason    Yes (default) allows typing. No = canned list
                            only.

Maximum reason length       Default 333 characters.

Canned reasons              One per line. Shown in the reason dialog.

Require the user to         Yes = they must re-enter their Windows password
re-enter their Windows      in the app after they click Grant. This is
password                    enforced in the UI, not by the service.


Remote
------
Allow requests from         Off by default. The service does not open the
remote computers            TCP listener unless a remote allowed list is
                            also set.

Remote allowed / denied     Who may request admin on this PC from another
entities                    computer. Required to open the TCP listener.

End remote sessions when    Yes (default) disconnects the remote session
rights expire               when time is up.

TCP service port            Default 808.

Include the Remote UI       Adds the "Make Me Admin Remote" shortcut.


Notes
-----
- Names can be DOMAIN\User, AzureAD\user@tenant, or a SID.
- Denied always wins.
- If Allowed entities is empty and the enrolled-user box is unchecked,
  the software default still allows every local user. The MSI builder
  asks you to click I Understand before creating that package. Always
  set one or the other for production.
- After install, restart the Make Me Admin service if you need to pick
  up a change without rebooting.
