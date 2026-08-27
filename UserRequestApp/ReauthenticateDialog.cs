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
    using System.Security.Principal;
    using System.Windows.Forms;

    /// <summary>
    /// Prompts the current user to re-enter their Windows password inside the application.
    /// </summary>
    internal partial class ReauthenticateDialog : Form
    {
        public ReauthenticateDialog()
        {
            InitializeComponent();

            this.Icon = Properties.Resources.SecurityLock;
            this.Text = Properties.Resources.CredentialsPromptCaption;
            this.messageLabel.Text = Properties.Resources.CredentialsPromptMessage;
            this.accountLabel.Text = Properties.Resources.AuthenticationAccountLabel;
            this.passwordLabel.Text = Properties.Resources.AuthenticationPasswordLabel;
            this.errorLabel.Text = Properties.Resources.AuthenticationIncorrectPassword;
            this.accountTextBox.Text = WindowsIdentity.GetCurrent().Name;
        }

        private void FormLoadHandler(object sender, EventArgs e)
        {
            this.passwordTextBox.Focus();
        }

        private void PasswordTextBoxChangedHandler(object sender, EventArgs e)
        {
            this.okButton.Enabled = this.passwordTextBox.Text.Length > 0;
            this.errorLabel.Visible = false;
        }

        private void OkButtonClickHandler(object sender, EventArgs e)
        {
            this.okButton.Enabled = false;
            this.cancelButton.Enabled = false;

            bool authenticated = NativeMethods.ValidateCurrentUserPassword(this.passwordTextBox.Text);
            if (authenticated)
            {
                this.DialogResult = DialogResult.OK;
                this.Close();
                return;
            }

            this.errorLabel.Visible = true;
            this.passwordTextBox.Clear();
            this.passwordTextBox.Focus();
            this.okButton.Enabled = false;
            this.cancelButton.Enabled = true;
        }
    }
}
