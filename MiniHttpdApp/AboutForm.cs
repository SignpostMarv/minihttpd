using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;

namespace MiniHttpdApp
{
	/// <summary>
	/// Summary description for AboutForm.
	/// </summary>
	public class AboutForm : System.Windows.Forms.Form
	{
		private System.Windows.Forms.LinkLabel linkLabel2;
		private System.Windows.Forms.Label label3;
		private System.Windows.Forms.LinkLabel linkLabel1;
		private System.Windows.Forms.Label versionLabel;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.Button buttonOK;
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;

		public AboutForm()
		{
			//
			// Required for Windows Form Designer support
			//
			InitializeComponent();

			versionLabel.Text = "Version " + typeof(MiniHttpd.HttpServer).Assembly.GetName().Version.ToString();
		}

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
			this.linkLabel2 = new System.Windows.Forms.LinkLabel();
			this.buttonOK = new System.Windows.Forms.Button();
			this.label3 = new System.Windows.Forms.Label();
			this.linkLabel1 = new System.Windows.Forms.LinkLabel();
			this.versionLabel = new System.Windows.Forms.Label();
			this.label1 = new System.Windows.Forms.Label();
			this.SuspendLayout();
			// 
			// linkLabel2
			// 
			this.linkLabel2.FlatStyle = System.Windows.Forms.FlatStyle.System;
			this.linkLabel2.Location = new System.Drawing.Point(8, 80);
			this.linkLabel2.Name = "linkLabel2";
			this.linkLabel2.Size = new System.Drawing.Size(136, 23);
			this.linkLabel2.TabIndex = 10;
			this.linkLabel2.TabStop = true;
			this.linkLabel2.Text = "rei@thefraser.com";
			this.linkLabel2.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.linkLabel2_LinkClicked);
			// 
			// buttonOK
			// 
			this.buttonOK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.buttonOK.DialogResult = System.Windows.Forms.DialogResult.OK;
			this.buttonOK.FlatStyle = System.Windows.Forms.FlatStyle.System;
			this.buttonOK.Location = new System.Drawing.Point(296, 104);
			this.buttonOK.Name = "buttonOK";
			this.buttonOK.TabIndex = 5;
			this.buttonOK.Text = "&OK";
			// 
			// label3
			// 
			this.label3.FlatStyle = System.Windows.Forms.FlatStyle.System;
			this.label3.Location = new System.Drawing.Point(8, 64);
			this.label3.Name = "label3";
			this.label3.Size = new System.Drawing.Size(360, 16);
			this.label3.TabIndex = 9;
			this.label3.Text = "Library and MiniHttpdApp sample application by Rei Miyasaka, 2005";
			// 
			// linkLabel1
			// 
			this.linkLabel1.FlatStyle = System.Windows.Forms.FlatStyle.System;
			this.linkLabel1.Location = new System.Drawing.Point(8, 40);
			this.linkLabel1.Name = "linkLabel1";
			this.linkLabel1.Size = new System.Drawing.Size(280, 16);
			this.linkLabel1.TabIndex = 8;
			this.linkLabel1.TabStop = true;
			this.linkLabel1.Text = "http://www.codeproject.com/csharp/minihttpd.asp";
			this.linkLabel1.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.linkLabel1_LinkClicked);
			// 
			// versionLabel
			// 
			this.versionLabel.FlatStyle = System.Windows.Forms.FlatStyle.System;
			this.versionLabel.Location = new System.Drawing.Point(8, 24);
			this.versionLabel.Name = "versionLabel";
			this.versionLabel.Size = new System.Drawing.Size(120, 16);
			this.versionLabel.TabIndex = 7;
			this.versionLabel.Text = "Version";
			// 
			// label1
			// 
			this.label1.FlatStyle = System.Windows.Forms.FlatStyle.System;
			this.label1.Location = new System.Drawing.Point(8, 8);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(216, 16);
			this.label1.TabIndex = 6;
			this.label1.Text = "MiniHttpd web server library";
			// 
			// AboutForm
			// 
			this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
			this.ClientSize = new System.Drawing.Size(376, 133);
			this.Controls.Add(this.linkLabel2);
			this.Controls.Add(this.buttonOK);
			this.Controls.Add(this.label3);
			this.Controls.Add(this.linkLabel1);
			this.Controls.Add(this.versionLabel);
			this.Controls.Add(this.label1);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "AboutForm";
			this.Text = "AboutForm";
			this.ResumeLayout(false);

		}
		#endregion

		private void linkLabel1_LinkClicked(object sender, System.Windows.Forms.LinkLabelLinkClickedEventArgs e)
		{
			System.Diagnostics.Process.Start((sender as LinkLabel).Text);
		}

		private void linkLabel2_LinkClicked(object sender, System.Windows.Forms.LinkLabelLinkClickedEventArgs e)
		{
			System.Diagnostics.Process.Start("mailto:" + (sender as LinkLabel).Text);
		}
	}
}
