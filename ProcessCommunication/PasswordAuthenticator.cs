// 
// Copyright © 2010-2019, Sinclair Community College
// Licensed under the GNU General Public License, version 3.
// See the LICENSE file in the project root for full license information.  
//
// This file is part of Make Me Admin.
//

namespace SinclairCC.MakeMeAdmin
{
    using System;
    using System.Runtime.InteropServices;
    using System.Security;
    using System.Security.Principal;
    using System.Text;

    /// <summary>
    /// Confirms a password belongs to a specific Windows identity via LogonUser.
    /// Used by the service so re-auth cannot be skipped by calling the named pipe directly.
    /// </summary>
    public static class PasswordAuthenticator
    {
        private const int LOGON32_PROVIDER_DEFAULT = 0;
        private const int LOGON32_LOGON_INTERACTIVE = 2;
        private const int LOGON32_LOGON_NETWORK = 3;
        private const int LOGON32_LOGON_NETWORK_CLEARTEXT = 8;

        private const int ERROR_LOGON_FAILURE = 1326;
        private const int ERROR_WRONG_PASSWORD = 1323;
        private const int ERROR_LOGON_TYPE_NOT_GRANTED = 1385;

        private const int NameUserPrincipal = 8;

        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern bool LogonUser(string username, string domain, IntPtr password, int logonType, int logonProvider, ref IntPtr token);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        private static extern bool CloseHandle(IntPtr handle);

        [DllImport("secur32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern int GetUserNameEx(int nameFormat, StringBuilder userName, ref int nSize);

        /// <summary>
        /// Returns true if <paramref name="password"/> logs on as <paramref name="userIdentity"/>.
        /// </summary>
        /// <remarks>
        /// Does not use Environment.UserName, which would be SYSTEM when called from the service.
        /// GetUserNameEx is used only when the identity is the process's current user (the local UI).
        /// </remarks>
        public static bool ValidatePassword(WindowsIdentity userIdentity, string password)
        {
            if (userIdentity == null || userIdentity.User == null || string.IsNullOrEmpty(password))
            {
                return false;
            }

            SplitAccount(userIdentity.Name, out string domain, out string userName);
            bool cloudIdentity = IsCloudIdentityDomain(domain);
            string upn = UpnFromIdentity(userIdentity, userName);

            int error;
            if (!cloudIdentity)
            {
                if (TryLogon(userName, domain, password, LOGON32_LOGON_INTERACTIVE, userIdentity.User, out error))
                {
                    return true;
                }

                if (error == ERROR_LOGON_TYPE_NOT_GRANTED)
                {
                    if (TryLogon(userName, domain, password, LOGON32_LOGON_NETWORK, userIdentity.User, out error))
                    {
                        return true;
                    }

                    if (error == ERROR_LOGON_TYPE_NOT_GRANTED &&
                        TryLogon(userName, domain, password, LOGON32_LOGON_NETWORK_CLEARTEXT, userIdentity.User, out error))
                    {
                        return true;
                    }
                }

                if (error == ERROR_LOGON_FAILURE || error == ERROR_WRONG_PASSWORD)
                {
                    return false;
                }
            }

            string machineName = Environment.MachineName;
            if (TryLogon(userName, machineName, password, LOGON32_LOGON_INTERACTIVE, userIdentity.User, out _))
            {
                return true;
            }

            if (!string.IsNullOrEmpty(upn) &&
                TryLogon(upn, string.Empty, password, LOGON32_LOGON_INTERACTIVE, userIdentity.User, out _))
            {
                return true;
            }

            if (!string.IsNullOrEmpty(upn) &&
                TryLogon(upn, string.Empty, password, LOGON32_LOGON_NETWORK, userIdentity.User, out _))
            {
                return true;
            }

            return false;
        }

        private static string UpnFromIdentity(WindowsIdentity userIdentity, string userName)
        {
            if (!string.IsNullOrEmpty(userName) && userName.IndexOf('@') >= 0)
            {
                return userName;
            }

            try
            {
                WindowsIdentity current = WindowsIdentity.GetCurrent();
                if (current != null && current.User == userIdentity.User)
                {
                    int size = 256;
                    StringBuilder buffer = new StringBuilder(size);
                    if (GetUserNameEx(NameUserPrincipal, buffer, ref size) != 0)
                    {
                        string currentUpn = buffer.ToString();
                        if (!string.IsNullOrEmpty(currentUpn))
                        {
                            return currentUpn;
                        }
                    }
                }
            }
            catch (SecurityException)
            {
            }

            return null;
        }

        private static bool IsCloudIdentityDomain(string domain)
        {
            return string.Equals(domain, "AzureAD", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(domain, "MicrosoftAccount", StringComparison.OrdinalIgnoreCase);
        }

        private static void SplitAccount(string accountName, out string domain, out string userName)
        {
            domain = string.Empty;
            userName = accountName ?? string.Empty;

            if (string.IsNullOrEmpty(accountName))
            {
                return;
            }

            int separatorIndex = accountName.IndexOf('\\');
            if (separatorIndex >= 0)
            {
                domain = accountName.Substring(0, separatorIndex);
                userName = accountName.Substring(separatorIndex + 1);
                return;
            }

            separatorIndex = accountName.IndexOf('@');
            if (separatorIndex >= 0)
            {
                userName = accountName;
                domain = string.Empty;
            }
        }

        private static bool TryLogon(string userName, string domain, string password, int logonType, SecurityIdentifier expectedSid, out int error)
        {
            error = 0;
            IntPtr tokenHandle = IntPtr.Zero;
            IntPtr passwordPtr = IntPtr.Zero;
            SecureString securePassword = null;

            try
            {
                securePassword = new SecureString();
                foreach (char character in password)
                {
                    securePassword.AppendChar(character);
                }
                securePassword.MakeReadOnly();
                passwordPtr = Marshal.SecureStringToGlobalAllocUnicode(securePassword);

                string logonDomain = string.IsNullOrEmpty(domain) ? null : domain;
                bool loggedOn = LogonUser(userName, logonDomain, passwordPtr, logonType, LOGON32_PROVIDER_DEFAULT, ref tokenHandle);
                if (!loggedOn)
                {
                    error = Marshal.GetLastWin32Error();
                    return false;
                }

                using (WindowsIdentity loggedOnIdentity = new WindowsIdentity(tokenHandle))
                {
                    return expectedSid != null && expectedSid.Equals(loggedOnIdentity.User);
                }
            }
            finally
            {
                if (passwordPtr != IntPtr.Zero)
                {
                    Marshal.ZeroFreeGlobalAllocUnicode(passwordPtr);
                }

                if (securePassword != null)
                {
                    securePassword.Dispose();
                }

                if (tokenHandle != IntPtr.Zero)
                {
                    CloseHandle(tokenHandle);
                }
            }
        }
    }
}
