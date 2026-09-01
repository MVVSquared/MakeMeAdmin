## What's Different in This Version

This repository is a fork of [Make Me Admin](https://github.com/pseymour/MakeMeAdmin) by Sinclair Community College / Patrick Seymour. The original application is unchanged in purpose: an authorized standard user can request temporary membership in the local Administrators group, a Windows service grants it, and the service takes it away when the timeout expires.

This build is **version 2.3.84**. It keeps the 2.3 product line and adds features for Entra-joined, Intune-managed PCs: who may request admin on a personally enrolled device, how the package is deployed, and how activity is sent to a campus log collector.

The upstream project has continued on its own path (currently 2.4.x). This page describes **this fork** versus the original 2.3 product and versus current upstream, not a claim that upstream is unmaintained.

### What is the same

These behaviors come from the original project and still work here:

- Grant and remove local Administrators membership through a background service.
- Allowed and denied users or groups (SIDs or `DOMAIN\Name`).
- Optional automatic add at logon, timeout overrides, renewals, and removal on logoff.
- Reason prompt (none / optional / required), canned reasons, and optional password re-entry.
- Syslog, Windows Event Log, and optional logging of elevated processes.
- Group Policy ADMX/ADML templates under the same registry keys as upstream.
- GPLv3 license. UAC must still be enabled for elevation prompts to work.

### What this fork adds

#### Entra / Intune enrolled user

A new setting, **Allow Enrolled User**, lets the Microsoft Entra ID user who enrolled the computer request administrator rights without being listed in Allowed Entities.

That identity comes from the device's Cloud Domain Join information (the "Enrolled by" user), not Intune Primary User. Denied Entities still win. The enrollee is **not** added to Administrators at logon; they still have to click Grant.

This is meant for individual, user-enrolled PCs. On labs and other shared computers, leave the setting off and use Allowed Entities instead.

When Allow Enrolled User is on and Allowed Entities is empty, **only the enrollee** may request rights. That is stricter than the original product, where an unset allow list means every local user may request rights.

#### HTTPS web logging

In addition to Event Log and syslog, the service can POST JSON log entries to an HTTPS URL (`WebLogEndpoint`). An optional API key is sent as an `X-Api-Key` header.

The key is **not** left in the registry. At install (or on first use, if an old plaintext value is still present), LocalSystem DPAPI-protects it and stores it at `%ProgramData%\Make Me Admin\weblog-apikey.bin`. Standard users cannot decrypt that blob. A local administrator can still recover it, as with any secret the service itself can use.

#### Intune-oriented installer

Upstream expects Group Policy or a separate configuration step after install. This fork can bake policy into the MSI:

- **MakeMeAdminMsiBuilder.exe** fills in the settings you want and writes a configured copy of the x64 MSI. Upload that file to Intune as a line-of-business app with no command-line arguments.
- The same values can be passed as **MSI properties** on the Intune command line (`WEBLOGENDPOINT`, `WEBLOGAPIKEY`, `ALLOWENROLLEDUSER`, `PROMPTFORREASON`, `ALLOWEDENTITIES`, and others). Details are in `deploy/README.md` in this repository.

The builder warns you before creating a package that leaves Allowed Entities empty and Allow Enrolled User off, because that is still the original "everyone may request" default.

#### Remote TCP listener

Upstream opens the remote TCP listener whenever Allow Remote Requests is enabled. This fork does **not** listen on TCP until Remote Allowed Entities is also set, so an incomplete remote policy cannot expose the service to every authenticated caller.

### Settings unique to this fork

These values are not in the original product. Store them in the same keys as the other settings:

`HKEY_LOCAL_MACHINE\SOFTWARE\Sinclair Community College\Make Me Admin`

or, to enforce them:

`HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Sinclair Community College\Make Me Admin`


| Setting | Default | Format | What it does |
| ------- | ------- | ------ | ------------ |
| Allow Enrolled User | false (0) | `REG_DWORD` | When 1, the Entra / Intune user who enrolled this PC may request administrator rights, in addition to Allowed Entities. Denied Entities still win. Does not auto-add at logon. |
| WebLogEndpoint | *empty* | `REG_SZ` | HTTPS URL that receives JSON log POSTs. Leave empty to disable web logging. |
| WebLogApiKey | *empty* | (not kept in the registry) | Optional API key. Pass it as the `WEBLOGAPIKEY` MSI property or, for older scripts, a temporary policy value. The service migrates a plaintext registry value into the DPAPI blob and deletes it. |


The Group Policy templates in this build include Allow Enrolled User. Web logging is configured with the MSI builder, MSI properties, or registry — not the ADMX file.

### Suggested deployment patterns

**Personally enrolled PCs.** Check "Automatically allow the Entra / Intune user who enrolled this device" in the MSI builder (or set `ALLOWENROLLEDUSER=1`). Optionally add IT staff to Allowed Entities. The enrollee and anyone on that list can request admin; everyone else cannot.

**Labs and shared PCs.** Leave Allow Enrolled User off. Put lab or IT accounts in Allowed Entities.

**Always set one or the other for production.** If Allowed Entities is empty and Allow Enrolled User is off, the original default still allows every local user to request rights.

### Compared with upstream 2.4

| | This fork (2.3.84) | Upstream ([pseymour/MakeMeAdmin](https://github.com/pseymour/MakeMeAdmin)) |
| --- | --- | --- |
| Lineage | 2.3 with campus additions | 2.4.x (2.4.1 as of November 2025) |
| Entra enrolled-user allow list | Yes | No |
| HTTPS web log + DPAPI API key | Yes | No |
| MSI builder / Intune MSI properties | Yes | No |
| Remote TCP requires an allow list | Yes | No (listens whenever remote requests are enabled) |
| Empty Allowed Entities | Everyone allowed, unless Allow Enrolled User is on (then enrollee only) | Everyone allowed |
| 32-bit installer | Still built | Dropped (2026) |
| Extra localizations in 2.4 | English, French, Danish (as in 2.3) | Adds German in 2.4.1 |

If you only need the original temporary-admin workflow on Active Directory PCs with Group Policy, upstream 2.4 is the maintained public project. Use this fork if you need Entra enrolment-based authorization, Intune-friendly packaging, or HTTPS log ingest.

### Credits and license

Make Me Admin was created by Patrick Seymour at Sinclair Community College and is released under the GNU General Public License, version 3. This fork keeps that license. The original contributor list is in `CONTRIBUTORS.md` at the repository root.

[Configuration Settings](registry-settings.md) · [home](/ "Make Me Admin home page")
