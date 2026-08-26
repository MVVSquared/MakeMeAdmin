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
    using System;
    using System.Text;
    using Microsoft.Deployment.WindowsInstaller;

    public class CustomActions
    {
        /// <summary>
        /// Copies the WEBLOGAPIKEY property into CustomActionData for the deferred action.
        /// </summary>
        [CustomAction]
        public static ActionResult SetProtectWebLogApiKeyData(Session session)
        {
            session.Log("Preparing web log API key for protected storage.");
            string apiKey = session["WEBLOGAPIKEY"] ?? string.Empty;
            session["ProtectWebLogApiKey"] = "APIKEY=" + Convert.ToBase64String(Encoding.UTF8.GetBytes(apiKey));
            return ActionResult.Success;
        }

        /// <summary>
        /// DPAPI-protects the web log API key as LocalSystem and writes the blob to disk.
        /// </summary>
        [CustomAction]
        public static ActionResult ProtectWebLogApiKey(Session session)
        {
            try
            {
                string encoded = session.CustomActionData["APIKEY"];
                if (string.IsNullOrEmpty(encoded))
                {
                    return ActionResult.Success;
                }

                string apiKey = Encoding.UTF8.GetString(Convert.FromBase64String(encoded));
                if (!string.IsNullOrEmpty(apiKey))
                {
                    WebLogApiKeyStore.ProtectAndSave(apiKey);
                }
            }
            catch (Exception e)
            {
                session.Log("Failed to store the web log API key.");
                session.Log(e.Message);
                return ActionResult.Failure;
            }

            return ActionResult.Success;
        }

        [CustomAction]
        public static ActionResult RemoveUserList(Session session)
        {
            // TODO: i18n
            session.Log(string.Format("Removing saved user list \"{0}\".", EncryptedSettings.SettingsFilePath));

            int tries = 5;
            TimeSpan sleepTime = new TimeSpan(0, 0, 5);
            while ((tries > 0) && (System.IO.File.Exists(EncryptedSettings.SettingsFilePath)))
            {
                try
                {
                    EncryptedSettings.RemoveAllSettings();
                }
                catch (Exception e)
                {
                    // TODO: i18n
                    session.Log("Error while trying to remove saved user list.");
                    session.Log(e.Message);
                }

                tries--;
                if (tries > 0)
                {
                    // TODO: i18n
                    session.Log(string.Format("{0:N0} tries remaining.", tries));
                    System.Threading.Thread.Sleep(sleepTime);
                }
            }

            try
            {
                WebLogApiKeyStore.RemoveBlob();
            }
            catch (Exception e)
            {
                session.Log("Error while trying to remove the web log API key blob.");
                session.Log(e.Message);
            }

            // TODO: i18n
            session.Log("Finished removing saved user list.");

            return ActionResult.Success;
        }
    }
}
