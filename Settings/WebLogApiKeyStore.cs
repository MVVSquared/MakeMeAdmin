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
    using System.IO;
    using System.Security.AccessControl;
    using System.Security.Cryptography;
    using System.Security.Principal;
    using System.Text;

    /// <summary>
    /// Stores the web log API key as a DPAPI-protected blob on disk.
    /// </summary>
    /// <remarks>
    /// Protect and unprotect must run as the same Windows identity. The
    /// Make Me Admin service and the installer custom action run as
    /// LocalSystem, so DataProtectionScope.CurrentUser is the SYSTEM
    /// DPAPI key — standard users cannot decrypt the blob.
    /// </remarks>
    public static class WebLogApiKeyStore
    {
        private const string BlobFileName = "weblog-apikey.bin";
        private const string RegistryValueName = "WebLogApiKey";

        /// <summary>
        /// Optional DPAPI entropy. Must stay stable or existing blobs cannot be read.
        /// </summary>
        private static readonly byte[] Entropy = Encoding.UTF8.GetBytes("MakeMeAdmin.WebLogApiKey.v1");

        /// <summary>
        /// Gets the full path of the protected API key blob.
        /// </summary>
        public static string BlobFilePath
        {
            get
            {
                string directoryPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                    "Make Me Admin");
                return Path.Combine(directoryPath, BlobFileName);
            }
        }

        /// <summary>
        /// Protects the API key with DPAPI and writes the blob to disk.
        /// </summary>
        /// <param name="plaintext">
        /// The API key in plaintext. If null or empty, any existing blob is removed.
        /// </param>
        public static void ProtectAndSave(string plaintext)
        {
            if (string.IsNullOrEmpty(plaintext))
            {
                RemoveBlob();
                return;
            }

            byte[] plaintextBytes = Encoding.UTF8.GetBytes(plaintext);
            try
            {
                byte[] blobBytes = ProtectedData.Protect(plaintextBytes, Entropy, DataProtectionScope.CurrentUser);
                string directoryPath = Path.GetDirectoryName(BlobFilePath);
                Directory.CreateDirectory(directoryPath);
                File.WriteAllBytes(BlobFilePath, blobBytes);
                RestrictBlobAcl();
            }
            finally
            {
                Array.Clear(plaintextBytes, 0, plaintextBytes.Length);
            }
        }

        /// <summary>
        /// Returns the API key, preferring a fresh plaintext registry value
        /// (Intune script), then the DPAPI blob.
        /// </summary>
        /// <returns>
        /// The API key, or null if none is configured or this identity cannot decrypt the blob.
        /// </returns>
        public static string Unprotect()
        {
            string registryKey = ReadPlaintextFromRegistry();
            if (!string.IsNullOrEmpty(registryKey))
            {
                try
                {
                    ProtectAndSave(registryKey);
                    DeletePlaintextFromRegistry();
                }
                catch (CryptographicException)
                {
                    // Another identity (not SYSTEM) cannot protect as CurrentUser for the service.
                }
                catch (UnauthorizedAccessException)
                {
                }
                catch (IOException)
                {
                }

                return registryKey;
            }

            if (!File.Exists(BlobFilePath))
            {
                return null;
            }

            try
            {
                byte[] blobBytes = File.ReadAllBytes(BlobFilePath);
                byte[] plaintextBytes = ProtectedData.Unprotect(blobBytes, Entropy, DataProtectionScope.CurrentUser);
                try
                {
                    return Encoding.UTF8.GetString(plaintextBytes);
                }
                finally
                {
                    Array.Clear(plaintextBytes, 0, plaintextBytes.Length);
                }
            }
            catch (CryptographicException)
            {
                return null;
            }
            catch (IOException)
            {
                return null;
            }
            catch (UnauthorizedAccessException)
            {
                return null;
            }
        }

        /// <summary>
        /// If a plaintext registry value is present, writes a blob and deletes the value.
        /// </summary>
        /// <returns>
        /// True if a registry value was migrated; otherwise, false.
        /// </returns>
        public static bool TryMigrateFromRegistry()
        {
            string registryKey = ReadPlaintextFromRegistry();
            if (string.IsNullOrEmpty(registryKey))
            {
                return false;
            }

            ProtectAndSave(registryKey);
            DeletePlaintextFromRegistry();
            return true;
        }

        /// <summary>
        /// Deletes the protected blob file if it exists.
        /// </summary>
        public static void RemoveBlob()
        {
            try
            {
                if (File.Exists(BlobFilePath))
                {
                    File.Delete(BlobFilePath);
                }
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
        }

        private static string ReadPlaintextFromRegistry()
        {
            try
            {
                object value = Registry.GetValue(
                    @"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Sinclair Community College\Make Me Admin",
                    RegistryValueName,
                    null);
                return value as string;
            }
            catch (System.Security.SecurityException)
            {
                return null;
            }
            catch (IOException)
            {
                return null;
            }
        }

        private static void DeletePlaintextFromRegistry()
        {
            try
            {
                using (RegistryKey key = Registry.LocalMachine.OpenSubKey(
                    @"SOFTWARE\Policies\Sinclair Community College\Make Me Admin",
                    writable: true))
                {
                    if (key != null)
                    {
                        key.DeleteValue(RegistryValueName, throwOnMissingValue: false);
                    }
                }
            }
            catch (System.Security.SecurityException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
            catch (IOException)
            {
            }
        }

        private static void RestrictBlobAcl()
        {
            try
            {
                var fileInfo = new FileInfo(BlobFilePath);
                var security = new FileSecurity();
                security.SetAccessRuleProtection(isProtected: true, preserveInheritance: false);
                security.AddAccessRule(new FileSystemAccessRule(
                    new SecurityIdentifier(WellKnownSidType.LocalSystemSid, null),
                    FileSystemRights.FullControl,
                    AccessControlType.Allow));
                security.AddAccessRule(new FileSystemAccessRule(
                    new SecurityIdentifier(WellKnownSidType.BuiltinAdministratorsSid, null),
                    FileSystemRights.FullControl,
                    AccessControlType.Allow));
                fileInfo.SetAccessControl(security);
            }
            catch (Exception)
            {
            }
        }
    }
}
