using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;
using MiniHttpd;

namespace MiniHttpdApp
{
	/// <summary>
	/// Summary description for EditUsersForm.
	/// </summary>
	public class EditUsersForm : System.Windows.Forms.Form
	{
		private System.Windows.Forms.ListBox userList;
		private System.Windows.Forms.Button buttonAdd;
		private System.Windows.Forms.Button buttonPassword;
		private System.Windows.Forms.Button buttonDelete;
		private System.Windows.Forms.CheckBox checkEnableAuthenticatoin;
		private System.Windows.Forms.Button buttonClose;
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;

		public EditUsersForm(BasicAuthenticator authenticator)
		{
			//
			// Required for Windows Form Designer support
			//
			InitializeComponent();

			this.authenticator = authenticator;
		}

		BasicAuthenticator authenticator;

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose( bool disposing )
		{
			if( disposing )
			{
				if(components != null)
				{
					components.Dispose();
				}
			}
			base.Dispose( disposing );
		}

		#region Windows Form Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.buttonClose = new System.Windows.Forms.Button();
			this.userList = new System.Windows.Forms.ListBox();
			this.buttonAdd = new System.Windows.Forms.Button();
			this.buttonPassword = new System.Windows.Forms.Button();
			this.buttonDelete = new System.Windows.Forms.Button();
			this.checkEnableAuthenticatoin = new System.Windows.Forms.CheckBox();
			this.SuspendLayout();
			// 
			// buttonClose
			// 
			this.buttonClose.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.buttonClose.DialogResult = System.Windows.Forms.DialogResult.OK;
			this.buttonClose.FlatStyle = System.Windows.Forms.FlatStyle.System;
			this.buttonClose.Location = new System.Drawing.Point(296, 288);
			this.buttonClose.Name = "buttonClose";
			this.buttonClose.TabIndex = 0;
			this.buttonClose.Text = "&Close";
			// 
			// userList
			// 
			this.userList.Location = new System.Drawing.Point(32, 32);
			this.userList.Name = "userList";
			this.userList.Size = new System.Drawing.Size(232, 225);
			this.userList.Sorted = true;
			this.userList.TabIndex = 2;
			// 
			// buttonAdd
			// 
			this.buttonAdd.FlatStyle = System.Windows.Forms.FlatStyle.System;
			this.buttonAdd.Location = new System.Drawing.Point(280, 32);
			this.buttonAdd.Name = "buttonAdd";
			this.buttonAdd.TabIndex = 3;
			this.buttonAdd.Text = "&Add";
			this.buttonAdd.Click += new System.EventHandler(this.buttonAdd_Click);
			// 
			// buttonPassword
			// 
			this.buttonPassword.FlatStyle = System.Windows.Forms.FlatStyle.System;
			this.buttonPassword.Location = new System.Drawing.Point(280, 72);
			this.buttonPassword.Name = "buttonPassword";
			this.buttonPassword.TabIndex = 4;
			this.buttonPassword.Text = "&Password";
			this.buttonPassword.Click += new System.EventHandler(this.buttonPassword_Click);
			// 
			// buttonDelete
			// 
			this.buttonDelete.FlatStyle = System.Windows.Forms.FlatStyle.System;
			this.buttonDelete.Location = new System.Drawing.Point(280, 112);
			this.buttonDelete.Name = "buttonDelete";
			this.buttonDelete.TabIndex = 5;
			this.buttonDelete.Text = "&Delete";
			this.buttonDelete.Click += new System.EventHandler(this.buttonDelete_Click);
			// 
			// checkEnableAuthenticatoin
			// 
			this.checkEnableAuthenticatoin.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.checkEnableAuthenticatoin.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
			this.checkEnableAuthenticatoin.Location = new System.Drawing.Point(128, 288);
			this.checkEnableAuthenticatoin.Name = "checkEnableAuthenticatoin";
			this.checkEnableAuthenticatoin.Size = new System.Drawing.Size(152, 24);
			this.checkEnableAuthenticatoin.TabIndex = 7;
			this.checkEnableAuthenticatoin.Text = "&Enable Authentication";
			// 
			// EditUsersForm
			// 
			this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
			this.ClientSize = new System.Drawing.Size(386, 315);
			this.Controls.Add(this.checkEnableAuthenticatoin);
			this.Controls.Add(this.buttonDelete);
			this.Controls.Add(this.buttonPassword);
			this.Controls.Add(this.buttonAdd);
			this.Controls.Add(this.userList);
			this.Controls.Add(this.buttonClose);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "EditUsersForm";
			this.Text = "Edit Users";
			this.Load += new System.EventHandler(this.EditUsersForm_Load);
			this.ResumeLayout(false);

		}
		#endregion

		private void buttonAdd_Click(object sender, System.EventArgs e)
		{
			TextBoxForm form = null;
			string username;
			try
			{
				form = new TextBoxForm();
				form.Text = "Enter Username";
				form.Caption = "Enter username:";
				if(form.ShowDialog(this) != DialogResult.OK)
					return;

				username = form.TextBox.Text;
				if(authenticator.Exists(username))
				{
					MessageBox.Show(this, "User already exists.");
					return;
				}
			}
			finally
			{
				if(form != null)
					form.Dispose();
			}

			try
			{
				form = new TextBoxForm();
				form.Text = "Enter password";
				form.Caption = "Enter password for " + username + ":";
				if(form.ShowDialog(this) != DialogResult.OK)
					return;

				string password = form.TextBox.Text;

				authenticator.AddUser(username, password);
				userList.Items.Add(username);
				userList.SelectedItem = username;
			}
			finally
			{
				if(form != null)
					form.Dispose();
			}
		}

		private void buttonPassword_Click(object sender, System.EventArgs e)
		{
			if(userList.SelectedIndex < 0)
				return;

			string user = userList.SelectedItem as string;
			string currentPass;
			string newPass;

			TextBoxForm form = null;
			try
			{
				form = new TextBoxForm();
				form.Text = "Enter current password";
				form.Caption = "Enter current password for user " + user + ":";
				if(form.ShowDialog(this) != DialogResult.OK)
					return;

				currentPass = form.TextBox.Text;

			}
			finally
			{
				if(form != null)
					form.Dispose();
			}

			try
			{
				form = new TextBoxForm();
				form.Text = "Enter new password:";
				form.Caption = "Enter new password for user " + user + ":";
				if(form.ShowDialog(this) != DialogResult.OK)
					return;

				newPass = form.TextBox.Text;
			}
			finally
			{
				if(form != null)
					form.Dispose();
			}

			try
			{
				authenticator.ChangePassword(user, currentPass, newPass);
			}
			catch(System.Security.SecurityException)
			{
				MessageBox.Show(this, "Password is incorrect.");
			}
		}

		private void buttonDelete_Click(object sender, System.EventArgs e)
		{
			if(userList.SelectedIndex < 0)
				return;

			string user = userList.SelectedItem as string;

			if(MessageBox.Show(this, string.Format("Are you sure you want to delete user {0}?", user),
				"Delete user?",
				MessageBoxButtons.YesNo,
				MessageBoxIcon.Question,
				MessageBoxDefaultButton.Button2) != DialogResult.Yes)
				return;

			authenticator.RemoveUser(user);
			userList.Items.Remove(user);
		}

		private void EditUsersForm_Load(object sender, System.EventArgs e)
		{
			foreach(string name in authenticator.Users)
				userList.Items.Add(name);
		}

		public bool EnableAuthentication
		{
			get
			{
				return checkEnableAuthenticatoin.Checked;
			}
		}
	}
}
