// 
// Copyright © 2010-2019, Sinclair Community College
// Licensed under the GNU General Public License, version 3.
// See the LICENSE file in the project root for full license information.  
//
// This file is part of Make Me Admin.
//
// Make Me Admin is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, version 3.
//
// Make Me Admin is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with Make Me Admin. If not, see <http://www.gnu.org/licenses/>.
//

namespace SinclairCC.MakeMeAdmin
{
    using Microsoft.Win32;
    using System;
    using System.Security.Principal;

    /// <summary>
    /// Identifies the Entra ID / Intune user who enrolled this computer.
    /// </summary>
    /// <remarks>
    /// User-driven Entra join stores the enrolled UPN in CloudDomainJoin JoinInfo.
    /// That value is the immutable "Enrolled by" user, not Intune Primary User.
    /// </remarks>
    public static class EnrolledDeviceUser
    {
        private const string JoinInfoKeyPath = @"SYSTEM\CurrentControlSet\Control\CloudDomainJoin\JoinInfo";

        private static readonly object CacheLock = new object();
        private static string cachedUpn;
        private static SecurityIdentifier cachedSid;

        /// <summary>
        /// Gets the UPN of the user who enrolled this device, if it is recorded locally.
        /// </summary>
        /// <returns>
        /// The enrolled user's UPN, or null if this computer is not Entra-joined
        /// with user affinity or the value is not present yet.
        /// </returns>
        public static string GetEnrolledUserPrincipalName()
        {
            try
            {
                using (RegistryKey joinInfo = Registry.LocalMachine.OpenSubKey(JoinInfoKeyPath))
                {
                    if (joinInfo == null)
                    {
                        return null;
                    }

                    foreach (string subKeyName in joinInfo.GetSubKeyNames())
                    {
                        using (RegistryKey subKey = joinInfo.OpenSubKey(subKeyName))
                        {
                            string userEmail = subKey?.GetValue("UserEmail") as string;
                            if (!string.IsNullOrWhiteSpace(userEmail))
                            {
                                return userEmail.Trim();
                            }
                        }
                    }
                }
            }
            catch (System.Security.SecurityException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }

            return null;
        }

        /// <summary>
        /// Returns true if the given identity is the user who enrolled this device.
        /// </summary>
        public static bool Matches(WindowsIdentity userIdentity)
        {
            if (userIdentity == null)
            {
                return false;
            }

            string upn = GetEnrolledUserPrincipalName();
            if (string.IsNullOrEmpty(upn))
            {
                return false;
            }

            if (NameMatchesUpn(userIdentity.Name, upn))
            {
                return true;
            }

            SecurityIdentifier enrolledSid = GetEnrolledUserSid(upn);
            return enrolledSid != null && userIdentity.User == enrolledSid;
        }

        private static SecurityIdentifier GetEnrolledUserSid(string upn)
        {
            lock (CacheLock)
            {
                if (cachedSid != null && string.Equals(cachedUpn, upn, StringComparison.OrdinalIgnoreCase))
                {
                    return cachedSid;
                }

                string[] candidates = new string[]
                {
                    "AzureAD\\" + upn,
                    upn,
                    "MicrosoftAccount\\" + upn
                };

                foreach (string candidate in candidates)
                {
                    SecurityIdentifier sid = LocalAdministratorGroup.GetSIDFromAccountName(candidate);
                    if (sid != null)
                    {
                        cachedUpn = upn;
                        cachedSid = sid;
                        return sid;
                    }
                }

                return null;
            }
        }

        private static bool NameMatchesUpn(string accountName, string upn)
        {
            if (string.IsNullOrEmpty(accountName) || string.IsNullOrEmpty(upn))
            {
                return false;
            }

            if (string.Equals(accountName, upn, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (string.Equals(accountName, "AzureAD\\" + upn, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(accountName, "MicrosoftAccount\\" + upn, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            int separatorIndex = accountName.LastIndexOf('\\');
            string tail = separatorIndex >= 0 ? accountName.Substring(separatorIndex + 1) : accountName;
            return string.Equals(tail, upn, StringComparison.OrdinalIgnoreCase);
        }
    }
}
