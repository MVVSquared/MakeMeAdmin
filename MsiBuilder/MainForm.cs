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
    using System.Drawing;
    using System.IO;
    using System.Linq;
    using System.Windows.Forms;

    internal sealed class MainForm : Form
    {
        private readonly TextBox webLogEndpointBox;
        private readonly TextBox apiKeyBox;
        private readonly CheckBox showApiKeyCheckBox;
        private readonly ComboBox logElevatedCombo;

        private readonly TextBox allowedEntitiesBox;
        private readonly TextBox deniedEntitiesBox;
        private readonly TextBox automaticAddAllowedBox;
        private readonly TextBox automaticAddDeniedBox;

        private readonly TextBox timeoutBox;
        private readonly TextBox renewalsBox;
        private readonly ComboBox removeOnLogoutCombo;
        private readonly ComboBox closeOnExpirationCombo;
        private readonly ComboBox overrideOutsideRemovalCombo;
        private readonly TextBox logOffAfterBox;
        private readonly TextBox logOffMessageBox;

        private readonly ComboBox promptForReasonCombo;
        private readonly ComboBox allowFreeFormCombo;
        private readonly TextBox maxReasonLengthBox;
        private readonly TextBox cannedReasonsBox;
        private readonly ComboBox requireAuthCombo;

        private readonly ComboBox allowRemoteCombo;
        private readonly TextBox remoteAllowedBox;
        private readonly TextBox remoteDeniedBox;
        private readonly ComboBox endRemoteSessionsCombo;
        private readonly TextBox tcpPortBox;
        private readonly CheckBox installRemoteUiCheckBox;

        private readonly Button createMsiButton;

        internal MainForm()
        {
            this.Text = "Make Me Admin MSI Builder";
            this.StartPosition = FormStartPosition.CenterScreen;
            this.MinimumSize = new Size(760, 580);
            this.Size = new Size(900, 720);
            this.Font = new Font("Segoe UI", 9F);
            this.AutoScaleMode = AutoScaleMode.Font;

            Label intro = new Label
            {
                AutoSize = true,
                Padding = new Padding(12, 10, 12, 6),
                Text = "Leave a field blank (or on “software default”) to omit that setting from the MSI. " +
                       "Filled-in values are written as policy so Intune can install the MSI with no extra command-line arguments."
            };

            TabControl tabs = new TabControl
            {
                Dock = DockStyle.Fill,
                Padding = new Point(8, 4),
                Margin = new Padding(8, 4, 8, 4)
            };

            tabs.TabPages.Add(CreateLoggingTab(
                out this.webLogEndpointBox,
                out this.apiKeyBox,
                out this.showApiKeyCheckBox,
                out this.logElevatedCombo));

            tabs.TabPages.Add(CreateAuthorizationTab(
                out this.allowedEntitiesBox,
                out this.deniedEntitiesBox,
                out this.automaticAddAllowedBox,
                out this.automaticAddDeniedBox));

            tabs.TabPages.Add(CreateSessionTab(
                out this.timeoutBox,
                out this.renewalsBox,
                out this.removeOnLogoutCombo,
                out this.closeOnExpirationCombo,
                out this.overrideOutsideRemovalCombo,
                out this.logOffAfterBox,
                out this.logOffMessageBox));

            tabs.TabPages.Add(CreateReasonTab(
                out this.promptForReasonCombo,
                out this.allowFreeFormCombo,
                out this.maxReasonLengthBox,
                out this.cannedReasonsBox,
                out this.requireAuthCombo));

            tabs.TabPages.Add(CreateRemoteTab(
                out this.allowRemoteCombo,
                out this.remoteAllowedBox,
                out this.remoteDeniedBox,
                out this.endRemoteSessionsCombo,
                out this.tcpPortBox,
                out this.installRemoteUiCheckBox));

            this.createMsiButton = new Button
            {
                Text = "Create MSI",
                Width = 160,
                Height = 36,
                Anchor = AnchorStyles.Right,
                UseVisualStyleBackColor = true
            };
            this.createMsiButton.Click += this.CreateMsiClick;

            Label footer = new Label
            {
                Dock = DockStyle.Fill,
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleLeft,
                Text = "The API key is stored inside the generated MSI. After install it is DPAPI-protected on the device. " +
                       "Anyone who has the MSI file can still extract the key."
            };

            TableLayoutPanel bottom = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                Padding = new Padding(12, 10, 12, 10)
            };
            bottom.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            bottom.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            bottom.Controls.Add(footer, 0, 0);
            bottom.Controls.Add(this.createMsiButton, 1, 0);

            TableLayoutPanel layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                Padding = new Padding(0)
            };
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 64F));
            intro.Dock = DockStyle.Fill;
            intro.AutoSize = true;
            layout.Controls.Add(intro, 0, 0);
            layout.Controls.Add(tabs, 0, 1);
            layout.Controls.Add(bottom, 0, 2);

            this.Controls.Add(layout);
            this.AcceptButton = this.createMsiButton;
        }

        private void CreateMsiClick(object sender, EventArgs e)
        {
            Dictionary<string, string> properties;
            try
            {
                properties = this.CollectProperties();
            }
            catch (FormatException ex)
            {
                MessageBox.Show(this, ex.Message, this.Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!string.IsNullOrEmpty(this.apiKeyBox.Text.Trim()) && string.IsNullOrEmpty(this.webLogEndpointBox.Text.Trim()))
            {
                DialogResult continueWithoutEndpoint = MessageBox.Show(
                    this,
                    "An API key is set but the web log endpoint is empty. Create the MSI anyway?",
                    this.Text,
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question,
                    MessageBoxDefaultButton.Button2);
                if (continueWithoutEndpoint != DialogResult.Yes)
                {
                    return;
                }
            }

            using (SaveFileDialog dialog = new SaveFileDialog())
            {
                dialog.Title = "Save configured Make Me Admin installer";
                dialog.Filter = "Windows Installer package (*.msi)|*.msi";
                dialog.DefaultExt = "msi";
                dialog.FileName = "MakeMeAdmin-configured.msi";
                dialog.OverwritePrompt = true;

                if (dialog.ShowDialog(this) != DialogResult.OK)
                {
                    return;
                }

                try
                {
                    this.createMsiButton.Enabled = false;
                    Cursor.Current = Cursors.WaitCursor;
                    MsiGenerator.CreateConfiguredMsi(dialog.FileName, properties);
                    MessageBox.Show(
                        this,
                        "Created:" + Environment.NewLine + dialog.FileName + Environment.NewLine + Environment.NewLine +
                        "Upload this file to Intune as a line-of-business MSI. Command-line arguments are not required.",
                        this.Text,
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        this,
                        "Could not create the MSI." + Environment.NewLine + Environment.NewLine + ex.Message,
                        this.Text,
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                    try
                    {
                        if (File.Exists(dialog.FileName))
                        {
                            File.Delete(dialog.FileName);
                        }
                    }
                    catch (IOException)
                    {
                    }
                    catch (UnauthorizedAccessException)
                    {
                    }
                }
                finally
                {
                    Cursor.Current = Cursors.Default;
                    this.createMsiButton.Enabled = true;
                }
            }
        }

        private Dictionary<string, string> CollectProperties()
        {
            Dictionary<string, string> properties = new Dictionary<string, string>(StringComparer.Ordinal);

            AddIfNotEmpty(properties, "WEBLOGENDPOINT", this.webLogEndpointBox.Text.Trim());
            AddIfNotEmpty(properties, "WEBLOGAPIKEY", this.apiKeyBox.Text.Trim());
            AddIfNotEmpty(properties, "LOGELEVATEDPROCESSES", SelectedNumericCombo(this.logElevatedCombo));

            AddIfNotEmpty(properties, "ALLOWEDENTITIES", JoinMultiString(this.allowedEntitiesBox.Text));
            AddIfNotEmpty(properties, "DENIEDENTITIES", JoinMultiString(this.deniedEntitiesBox.Text));
            AddIfNotEmpty(properties, "AUTOMATICADDALLOWED", JoinMultiString(this.automaticAddAllowedBox.Text));
            AddIfNotEmpty(properties, "AUTOMATICADDDENIED", JoinMultiString(this.automaticAddDeniedBox.Text));

            AddIfNotEmpty(properties, "ADMINRIGHTSTIMEOUT", ParseOptionalInteger(this.timeoutBox.Text, "Admin rights timeout", 1, 1440));
            AddIfNotEmpty(properties, "RENEWALSALLOWED", ParseOptionalInteger(this.renewalsBox.Text, "Renewals allowed", 0, 128));
            AddIfNotEmpty(properties, "REMOVEADMINRIGHTSONLOGOUT", SelectedYesNo(this.removeOnLogoutCombo));
            AddIfNotEmpty(properties, "CLOSEAPPLICATIONUPONEXPIRATION", SelectedYesNo(this.closeOnExpirationCombo));
            AddIfNotEmpty(properties, "OVERRIDEREMOVALBYOUTSIDEPROCESS", SelectedYesNo(this.overrideOutsideRemovalCombo));
            AddIfNotEmpty(properties, "LOGOFFAFTEREXPIRATION", ParseOptionalInteger(this.logOffAfterBox.Text, "Log off after expiration", 0, int.MaxValue));
            AddIfNotEmpty(properties, "LOGOFFMESSAGE", JoinMultiString(this.logOffMessageBox.Text));

            AddIfNotEmpty(properties, "PROMPTFORREASON", SelectedNumericCombo(this.promptForReasonCombo));
            AddIfNotEmpty(properties, "ALLOWFREEFORMREASON", SelectedYesNo(this.allowFreeFormCombo));
            AddIfNotEmpty(properties, "MAXIMUMREASONLENGTH", ParseOptionalInteger(this.maxReasonLengthBox.Text, "Maximum reason length", 1, 32767));
            AddIfNotEmpty(properties, "CANNEDREASONS", JoinMultiString(this.cannedReasonsBox.Text));
            AddIfNotEmpty(properties, "REQUIREAUTHENTICATIONFORPRIVILEGES", SelectedYesNo(this.requireAuthCombo));

            AddIfNotEmpty(properties, "ALLOWREMOTEREQUESTS", SelectedYesNo(this.allowRemoteCombo));
            AddIfNotEmpty(properties, "REMOTEALLOWEDENTITIES", JoinMultiString(this.remoteAllowedBox.Text));
            AddIfNotEmpty(properties, "REMOTEDENIEDENTITIES", JoinMultiString(this.remoteDeniedBox.Text));
            AddIfNotEmpty(properties, "ENDREMOTESESSIONSUPONEXPIRATION", SelectedYesNo(this.endRemoteSessionsCombo));
            AddIfNotEmpty(properties, "TCPSERVICEPORT", ParseOptionalInteger(this.tcpPortBox.Text, "TCP service port", 1, 65535));

            if (this.installRemoteUiCheckBox.Checked)
            {
                properties["INSTALLREMOTE"] = "1";
            }

            return properties;
        }

        private static void AddIfNotEmpty(IDictionary<string, string> properties, string name, string value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                properties[name] = value;
            }
        }

        private static string JoinMultiString(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return null;
            }

            string[] lines = text
                .Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries)
                .Select(line => line.Trim())
                .Where(line => line.Length > 0)
                .ToArray();

            if (lines.Length == 0)
            {
                return null;
            }

            return string.Join("[~]", lines);
        }

        private static string ParseOptionalInteger(string text, string fieldName, int min, int max)
        {
            string trimmed = (text ?? string.Empty).Trim();
            if (trimmed.Length == 0)
            {
                return null;
            }

            int value;
            if (!int.TryParse(trimmed, out value) || value < min || value > max)
            {
                throw new FormatException(string.Format("{0} must be a whole number between {1} and {2}.", fieldName, min, max));
            }

            return value.ToString();
        }

        private static string SelectedYesNo(ComboBox combo)
        {
            switch (combo.SelectedIndex)
            {
                case 1:
                    return "1";
                case 2:
                    return "0";
                default:
                    return null;
            }
        }

        private static string SelectedNumericCombo(ComboBox combo)
        {
            TaggedOption option = combo.SelectedItem as TaggedOption;
            if (option == null || option.Value == null)
            {
                return null;
            }

            return option.Value;
        }

        private static TabPage CreateLoggingTab(out TextBox endpointBox, out TextBox apiKeyBox, out CheckBox showKey, out ComboBox logElevated)
        {
            FlowLayoutPanel panel = CreateScrollPanel();
            AddHeading(panel, "Web logging");
            AddNote(panel, "HTTPS URL that receives JSON log POSTs. Omit both fields to disable web logging.");
            endpointBox = AddTextBox(panel, "Web log endpoint (for example https://logs.example.edu/ingest)");
            apiKeyBox = AddTextBox(panel, "API key (optional)");
            apiKeyBox.UseSystemPasswordChar = true;

            showKey = new CheckBox { Text = "Show API key", AutoSize = true, Margin = new Padding(0, 0, 0, 12) };
            CheckBox showKeyBox = showKey;
            TextBox keyBox = apiKeyBox;
            showKeyBox.CheckedChanged += (s, e) => { keyBox.UseSystemPasswordChar = !showKeyBox.Checked; };
            panel.Controls.Add(showKeyBox);

            AddHeading(panel, "Elevated process logging");
            logElevated = AddTaggedCombo(
                panel,
                "Log elevated processes",
                new TaggedOption("(software default — never)", null),
                new TaggedOption("Never", "0"),
                new TaggedOption("Only when the user is an administrator", "1"),
                new TaggedOption("Always", "2"));

            return WrapTab("Logging", panel);
        }

        private static TabPage CreateAuthorizationTab(out TextBox allowed, out TextBox denied, out TextBox autoAllowed, out TextBox autoDenied)
        {
            FlowLayoutPanel panel = CreateScrollPanel();
            AddNote(panel, "One SID or name per line (for example DOMAIN\\HelpDesk or S-1-5-32-545). Denied entries take precedence. An empty allowed list in policy means nobody is allowed; leaving these boxes empty leaves the software default (everyone allowed).");
            allowed = AddMultiline(panel, "Allowed entities", 5);
            denied = AddMultiline(panel, "Denied entities", 4);
            autoAllowed = AddMultiline(panel, "Automatically add at logon (allowed)", 4);
            autoDenied = AddMultiline(panel, "Automatically add at logon (denied)", 3);
            return WrapTab("Authorization", panel);
        }

        private static TabPage CreateSessionTab(
            out TextBox timeout,
            out TextBox renewals,
            out ComboBox removeOnLogout,
            out ComboBox closeOnExpiration,
            out ComboBox overrideOutside,
            out TextBox logOffAfter,
            out TextBox logOffMessage)
        {
            FlowLayoutPanel panel = CreateScrollPanel();
            timeout = AddTextBox(panel, "Admin rights timeout (minutes, default 10)");
            renewals = AddTextBox(panel, "Renewals allowed (default 0)");
            removeOnLogout = AddYesNoCombo(panel, "Remove admin rights on logout");
            closeOnExpiration = AddYesNoCombo(panel, "Close the application when rights expire");
            overrideOutside = AddYesNoCombo(panel, "Put the user back in Administrators if another process removes them");
            logOffAfter = AddTextBox(panel, "Log the user off this many seconds after rights expire (0 or blank = do not log off)");
            logOffMessage = AddMultiline(panel, "Log-off warning message (one line per paragraph)", 3);
            return WrapTab("Session", panel);
        }

        private static TabPage CreateReasonTab(
            out ComboBox prompt,
            out ComboBox freeForm,
            out TextBox maxLength,
            out TextBox canned,
            out ComboBox requireAuth)
        {
            FlowLayoutPanel panel = CreateScrollPanel();
            prompt = AddTaggedCombo(
                panel,
                "Prompt for a reason",
                new TaggedOption("(software default — none)", null),
                new TaggedOption("None", "0"),
                new TaggedOption("Optional", "1"),
                new TaggedOption("Required", "2"));
            freeForm = AddYesNoCombo(panel, "Allow a free-form reason");
            maxLength = AddTextBox(panel, "Maximum reason length (default 333)");
            canned = AddMultiline(panel, "Canned reasons (one per line)", 5);
            requireAuth = AddYesNoCombo(panel, "Require the user to re-authenticate before elevation");
            return WrapTab("Reason", panel);
        }

        private static TabPage CreateRemoteTab(
            out ComboBox allowRemote,
            out TextBox remoteAllowed,
            out TextBox remoteDenied,
            out ComboBox endRemote,
            out TextBox tcpPort,
            out CheckBox installRemoteUi)
        {
            FlowLayoutPanel panel = CreateScrollPanel();
            allowRemote = AddYesNoCombo(panel, "Allow requests from remote computers");
            remoteAllowed = AddMultiline(panel, "Remote allowed entities", 4);
            remoteDenied = AddMultiline(panel, "Remote denied entities", 3);
            endRemote = AddYesNoCombo(panel, "End remote sessions when rights expire");
            tcpPort = AddTextBox(panel, "TCP service port (default 808)");
            installRemoteUi = new CheckBox
            {
                Text = "Include the Remote UI in this installer",
                AutoSize = true,
                Margin = new Padding(0, 8, 0, 12)
            };
            panel.Controls.Add(installRemoteUi);
            return WrapTab("Remote", panel);
        }

        private static FlowLayoutPanel CreateScrollPanel()
        {
            return new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                Padding = new Padding(12)
            };
        }

        private static TabPage WrapTab(string title, Control content)
        {
            TabPage page = new TabPage(title);
            page.Controls.Add(content);
            page.Resize += (s, e) =>
            {
                FlowLayoutPanel flow = content as FlowLayoutPanel;
                if (flow != null)
                {
                    int width = Math.Max(200, page.ClientSize.Width - 36);
                    foreach (Control child in flow.Controls)
                    {
                        TextBox textBox = child as TextBox;
                        if (textBox != null)
                        {
                            textBox.Width = width;
                        }

                        ComboBox combo = child as ComboBox;
                        if (combo != null)
                        {
                            combo.Width = Math.Min(420, width);
                        }
                    }
                }
            };
            return page;
        }

        private static void AddHeading(FlowLayoutPanel panel, string text)
        {
            panel.Controls.Add(new Label
            {
                Text = text,
                AutoSize = true,
                Font = new Font(panel.Font, FontStyle.Bold),
                Margin = new Padding(0, 8, 0, 4)
            });
        }

        private static void AddNote(FlowLayoutPanel panel, string text)
        {
            panel.Controls.Add(new Label
            {
                Text = text,
                AutoSize = true,
                MaximumSize = new Size(820, 0),
                ForeColor = Color.FromArgb(64, 64, 64),
                Margin = new Padding(0, 0, 0, 8)
            });
        }

        private static TextBox AddTextBox(FlowLayoutPanel panel, string label)
        {
            panel.Controls.Add(new Label { Text = label, AutoSize = true, Margin = new Padding(0, 6, 0, 2) });
            TextBox box = new TextBox
            {
                Width = 780,
                Margin = new Padding(0, 0, 0, 8)
            };
            panel.Controls.Add(box);
            return box;
        }

        private static TextBox AddMultiline(FlowLayoutPanel panel, string label, int lines)
        {
            panel.Controls.Add(new Label { Text = label, AutoSize = true, Margin = new Padding(0, 6, 0, 2) });
            TextBox box = new TextBox
            {
                Width = 780,
                Multiline = true,
                AcceptsReturn = true,
                ScrollBars = ScrollBars.Vertical,
                Height = Math.Max(60, lines * 22),
                Font = new Font("Consolas", 9F),
                Margin = new Padding(0, 0, 0, 8)
            };
            panel.Controls.Add(box);
            return box;
        }

        private static ComboBox AddYesNoCombo(FlowLayoutPanel panel, string label)
        {
            return AddTaggedCombo(
                panel,
                label,
                new TaggedOption("(software default)", null),
                new TaggedOption("Yes", "1"),
                new TaggedOption("No", "0"));
        }

        private static ComboBox AddTaggedCombo(FlowLayoutPanel panel, string label, params TaggedOption[] options)
        {
            panel.Controls.Add(new Label { Text = label, AutoSize = true, Margin = new Padding(0, 6, 0, 2) });
            ComboBox combo = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Width = 420,
                Margin = new Padding(0, 0, 0, 8)
            };
            combo.Items.AddRange(options);
            combo.SelectedIndex = 0;
            panel.Controls.Add(combo);
            return combo;
        }

        private sealed class TaggedOption
        {
            internal TaggedOption(string text, string value)
            {
                this.Text = text;
                this.Value = value;
            }

            internal string Text { get; }

            internal string Value { get; }

            public override string ToString()
            {
                return this.Text;
            }
        }
    }
}
