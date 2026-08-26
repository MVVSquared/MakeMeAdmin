// 
// Copyright © 2010-2019, Sinclair Community College
// Licensed under the GNU General Public License, version 3.
// See the LICENSE file in the project root for full license information.  
//
// This file is part of Make Me Admin.
//

namespace SinclairCC.MakeMeAdmin.MsiBuilder
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Reflection;
    using System.Text.RegularExpressions;

    /// <summary>
    /// Copies the embedded template MSI and writes public properties into its Property table.
    /// </summary>
    internal static class MsiGenerator
    {
        internal const string EmbeddedResourceName = "MakeMeAdmin.msi";

        private const int MsiOpenDatabaseModeTransact = 1;
        private static readonly Regex PropertyNamePattern = new Regex(@"^[A-Z][A-Z0-9_]*$", RegexOptions.Compiled);

        internal static void CreateConfiguredMsi(string destinationPath, IDictionary<string, string> properties)
        {
            if (string.IsNullOrWhiteSpace(destinationPath))
            {
                throw new ArgumentException("A destination path is required.", nameof(destinationPath));
            }

            ExtractTemplate(destinationPath);
            ApplyProperties(destinationPath, properties ?? new Dictionary<string, string>());
        }

        internal static void ExtractTemplate(string destinationPath)
        {
            string directory = Path.GetDirectoryName(destinationPath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            Assembly assembly = Assembly.GetExecutingAssembly();
            using (Stream stream = assembly.GetManifestResourceStream(EmbeddedResourceName))
            {
                if (stream == null)
                {
                    throw new InvalidOperationException("The template MSI is not embedded in this executable. Rebuild MsiBuilder after the Setup project has produced an installer.");
                }

                using (FileStream output = File.Create(destinationPath))
                {
                    stream.CopyTo(output);
                }
            }
        }

        private static void ApplyProperties(string msiPath, IDictionary<string, string> properties)
        {
            Type installerType = Type.GetTypeFromProgID("WindowsInstaller.Installer");
            if (installerType == null)
            {
                throw new InvalidOperationException("Windows Installer is not available on this computer.");
            }

            object installer = Activator.CreateInstance(installerType);
            object database = installerType.InvokeMember(
                "OpenDatabase",
                BindingFlags.InvokeMethod,
                null,
                installer,
                new object[] { msiPath, MsiOpenDatabaseModeTransact });

            try
            {
                foreach (KeyValuePair<string, string> pair in properties)
                {
                    if (string.IsNullOrEmpty(pair.Value))
                    {
                        continue;
                    }

                    SetProperty(database, pair.Key, pair.Value);
                }

                database.GetType().InvokeMember("Commit", BindingFlags.InvokeMethod, null, database, null);
            }
            finally
            {
                ReleaseComObject(database);
                ReleaseComObject(installer);
            }
        }

        private static void SetProperty(object database, string name, string value)
        {
            if (!PropertyNamePattern.IsMatch(name))
            {
                throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, "Invalid MSI property name '{0}'.", name), nameof(name));
            }

            ExecuteSql(database, string.Format(CultureInfo.InvariantCulture, "DELETE FROM `Property` WHERE `Property`='{0}'", name));
            ExecuteSql(
                database,
                string.Format(
                    CultureInfo.InvariantCulture,
                    "INSERT INTO `Property` (`Property`,`Value`) VALUES ('{0}','{1}')",
                    name,
                    EscapeSql(value)));
        }

        private static void ExecuteSql(object database, string sql)
        {
            object view = database.GetType().InvokeMember(
                "OpenView",
                BindingFlags.InvokeMethod,
                null,
                database,
                new object[] { sql });

            try
            {
                view.GetType().InvokeMember("Execute", BindingFlags.InvokeMethod, null, view, new object[] { null });
            }
            finally
            {
                try
                {
                    view.GetType().InvokeMember("Close", BindingFlags.InvokeMethod, null, view, null);
                }
                catch (Exception)
                {
                }

                ReleaseComObject(view);
            }
        }

        private static string EscapeSql(string value)
        {
            return value.Replace("'", "''");
        }

        private static void ReleaseComObject(object comObject)
        {
            if (comObject == null)
            {
                return;
            }

            try
            {
                System.Runtime.InteropServices.Marshal.FinalReleaseComObject(comObject);
            }
            catch (Exception)
            {
            }
        }
    }
}
