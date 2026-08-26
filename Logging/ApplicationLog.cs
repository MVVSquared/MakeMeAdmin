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
    using System.Threading.Tasks;
    using System.Net.Http;
    using System.Text;
    using System.Diagnostics;

    /// <summary>
    /// This class allows simple logging of application events.
    /// </summary>
    public class ApplicationLog
    {
        /// <summary>
        /// The source name to use when writing events to the Event Log.
        /// </summary>
        private const string SourceName = "Make Me Admin";

        /// <summary>
        /// An EventLog object for interacting with this service's event log.
        /// </summary>
        private static System.Diagnostics.EventLog log;

        /// <summary>
        /// Constructor
        /// </summary>
        static ApplicationLog()
        {
            // Get an EventLog object for this service's log.
            log = new System.Diagnostics.EventLog(Properties.Resources.ApplicationLogName)
            {
                // Specify the source name for this event log.
                Source = SourceName
            };
        }

        /// <summary>
        /// Adds an event source to the event log on the local computer.
        /// </summary>
        public static void CreateSource()
        {
            // If the specified source name does not exist, create it.
            if (!System.Diagnostics.EventLog.SourceExists(SourceName))
            {
                System.Diagnostics.EventLog.CreateEventSource(SourceName, Properties.Resources.ApplicationLogName);
            }
        }

        /// <summary>
        /// Removes, from the local computer, the event source for this service.
        /// </summary>
        public static void RemoveSource()
        {
            // If the specified source name exists, remove it.
            if (System.Diagnostics.EventLog.SourceExists(SourceName))
            {
                System.Diagnostics.EventLog.DeleteEventSource(SourceName);
            }
        }

        /*
        /// <summary>
        /// Writes the specified message to the event log as an information event
        /// with the specified event ID.
        /// </summary>
        /// <param name="message">
        /// The message to be written to the log.
        /// </param>
        /// <param name="id">
        /// The event ID to use for the event being written.
        /// </param>
        public static void WriteInformationEvent(string message, EventID id)
        {
            log.WriteEntry(message, System.Diagnostics.EventLogEntryType.Information, (int)id);
            //System.Diagnostics.EventLogEntryType.

            int j = 0;
            Task[] tasks = new Task[Settings.SyslogServers.Count];

            foreach (SyslogServerInfo serverInfo in Settings.SyslogServers)
            {
                if (serverInfo.IsValid)
                {
                    Syslog syslog = new Syslog(serverInfo.Hostname, serverInfo.Port, serverInfo.Protocol, serverInfo.RFC);
                    tasks[j] = Task.Factory.StartNew(() => syslog.SendMessage(message, id.ToString(), SyslogNet.Client.Severity.Informational));
                }
                j++;
            }
        }

        /// <summary>
        /// Writes the specified message to the event log as an error event with the
        /// specified event ID.
        /// </summary>
        /// <param name="message">
        /// The message to be written to the log.
        /// </param>
        /// <param name="id">
        /// The event ID to use for the event being written.
        /// </param>
        public static void WriteErrorEvent(string message, EventID id)
        {
            log.WriteEntry(message, System.Diagnostics.EventLogEntryType.Error, (int)id);

            int j = 0;
            Task[] tasks = new Task[Settings.SyslogServers.Count];

            foreach (SyslogServerInfo serverInfo in Settings.SyslogServers)
            {
                if (serverInfo.IsValid)
                {
                    Syslog syslog = new Syslog(serverInfo.Hostname, serverInfo.Port, serverInfo.Protocol, serverInfo.RFC);
                    tasks[j] = Task.Factory.StartNew(() => syslog.SendMessage(message, id.ToString(), SyslogNet.Client.Severity.Error));
                }
                j++;
            }
        }

        /// <summary>
        /// Writes the specified message to the event log as a warning event with the
        /// specified event ID.
        /// </summary>
        /// <param name="message">
        /// The message to be written to the log.
        /// </param>
        /// <param name="id">
        /// The event ID to use for the event being written.
        /// </param>
        public static void WriteWarningEvent(string message, EventID id)
        {
            log.WriteEntry(message, System.Diagnostics.EventLogEntryType.Warning, (int)id);

            int j = 0;
            Task[] tasks = new Task[Settings.SyslogServers.Count];

            foreach (SyslogServerInfo serverInfo in Settings.SyslogServers)
            {
                if (serverInfo.IsValid)
                {
                    Syslog syslog = new Syslog(serverInfo.Hostname, serverInfo.Port, serverInfo.Protocol, serverInfo.RFC);
                    tasks[j] = Task.Factory.StartNew(() => syslog.SendMessage(message, id.ToString(), SyslogNet.Client.Severity.Warning));
                }
                j++;
            }
        }
        */

        /// <summary>
        /// Writes an event to the log.
        /// </summary>
        /// <param name="message">
        /// The message to write to the log.
        /// </param>
        /// <param name="id">
        /// An ID for the type of event being logged.
        /// </param>
        /// <param name="entryType">
        /// The severity of the message being logged (information, warning, etc.).
        /// </param>
        public static void WriteEvent(string message, EventID id, System.Diagnostics.EventLogEntryType entryType)
        {
            log.WriteEntry(message, entryType, (int)id);

            int j = 0;
            Task[] tasks = new Task[Settings.SyslogServers.Count];

            // Determine the syslog severity for this event, based on the event log entry type.
            SyslogNet.Client.Severity severity = SyslogNet.Client.Severity.Informational;
            switch (entryType)
            {
                case System.Diagnostics.EventLogEntryType.Error:
                    severity = SyslogNet.Client.Severity.Error;
                    break;
                case System.Diagnostics.EventLogEntryType.Warning:
                    severity = SyslogNet.Client.Severity.Warning;
                    break;
                case System.Diagnostics.EventLogEntryType.FailureAudit:
                    severity = SyslogNet.Client.Severity.Alert;
                    break;
                case System.Diagnostics.EventLogEntryType.SuccessAudit:
                    severity = SyslogNet.Client.Severity.Notice;
                    break;
                default:
                    break;
            }

            foreach (SyslogServerInfo serverInfo in Settings.SyslogServers)
            {
                if (serverInfo.IsValid)
                {
                    Syslog syslog = new Syslog(serverInfo.Hostname, serverInfo.Port, serverInfo.Protocol, serverInfo.RFC);
                    tasks[j] = Task.Factory.StartNew(() => syslog.SendMessage(message, id.ToString(), severity));
                }
                j++;
            }

            // Web logging: send to web endpoint if configured
            if (!string.IsNullOrEmpty(Settings.WebLogEndpoint))
            {
                // Fire-and-forget
                _ = SendWebLogAsync(Settings.WebLogEndpoint, message, id, severity.ToString());
            }
        }

        // Add this method to send logs to a web endpoint
        private static async Task SendWebLogAsync(string endpoint, string message, EventID id, string severity)
        {
            using (var client = new HttpClient())
            {
                // Add API key header if configured
                string apiKey = Settings.WebLogApiKey;
                if (!string.IsNullOrEmpty(apiKey))
                {
                    client.DefaultRequestHeaders.Add("X-Api-Key", apiKey);
                }

                var content = new StringContent(
                    $"{{\"message\": \"{message.Replace("\"", "\\\"")}\", \"eventId\": \"{id}\", \"severity\": \"{severity}\"}}",
                    Encoding.UTF8,
                    "application/json"
                );
                try
                {
                    System.Diagnostics.Debug.WriteLine($"Sending web log to: {endpoint}");
                    System.Diagnostics.Debug.WriteLine($"Request content: {await content.ReadAsStringAsync()}");
                    
                    var response = await client.PostAsync(endpoint, content);
                    System.Diagnostics.Debug.WriteLine($"Response status: {response.StatusCode}");
                    System.Diagnostics.Debug.WriteLine($"Response content: {await response.Content.ReadAsStringAsync()}");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error sending web log: {ex.Message}");
                    System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                }
            }
        }

        public static void WriteLogEntry(string message, EventLogEntryType entryType, int eventId)
        {
            try
            {
                // Write debug message to Event Log
                string debugMessage = $"DEBUG: {message}";
                log.WriteEntry(debugMessage, entryType, eventId);
                System.Diagnostics.Debug.WriteLine(debugMessage);

                // If a web logging endpoint has been configured, send the log entry there
                if (!string.IsNullOrEmpty(Settings.WebLogEndpoint))
                {
                    string webDebugMessage = $"DEBUG: Attempting to send log to web endpoint: {Settings.WebLogEndpoint}";
                    log.WriteEntry(webDebugMessage, entryType, eventId);
                    System.Diagnostics.Debug.WriteLine(webDebugMessage);
                    
                    // Fire-and-forget
                    _ = SendWebLogAsync(Settings.WebLogEndpoint, message, (EventID)eventId, entryType.ToString());
                }
                else
                {
                    string noEndpointMessage = "DEBUG: No web logging endpoint configured";
                    log.WriteEntry(noEndpointMessage, entryType, eventId);
                    System.Diagnostics.Debug.WriteLine(noEndpointMessage);
                }
            }
            catch (Exception ex)
            {
                string errorMessage = $"DEBUG: Error in WriteLogEntry: {ex.Message}";
                log.WriteEntry(errorMessage, EventLogEntryType.Error, eventId);
                System.Diagnostics.Debug.WriteLine(errorMessage);
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }
    }
}
