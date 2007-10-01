using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;

namespace MiniHttpdApp
{
	/// <summary>
	/// Summary description for NewRedirectForm.
	/// </summary>
	public class NewRedirectForm : System.Windows.Forms.Form
	{
		private System.Windows.Forms.TextBox nameBox;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.TextBox targetBox;
		private System.Windows.Forms.Button buttonOK;
		private System.Windows.Forms.Button buttonCancel;
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;

		public NewRedirectForm()
		{
			//
			// Required for Windows Form Designer support
			//
			InitializeComponent();

			//
			// TODO: Add any constructor code after InitializeComponent call
			//
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
			this.nameBox = new System.Windows.Forms.TextBox();
			this.label1 = new System.Windows.Forms.Label();
			this.label2 = new System.Windows.Forms.Label();
			this.targetBox = new System.Windows.Forms.TextBox();
			this.buttonOK = new System.Windows.Forms.Button();
			this.buttonCancel = new System.Windows.Forms.Button();
			this.SuspendLayout();
			// 
			// nameBox
			// 
			this.nameBox.Location = new System.Drawing.Point(120, 8);
			this.nameBox.Name = "nameBox";
			this.nameBox.Size = new System.Drawing.Size(336, 20);
			this.nameBox.TabIndex = 1;
			this.nameBox.Text = "";
			this.nameBox.KeyDown += new System.Windows.Forms.KeyEventHandler(this.NewRedirectForm_KeyDown);
			// 
			// label1
			// 
			this.label1.FlatStyle = System.Windows.Forms.FlatStyle.System;
			this.label1.Location = new System.Drawing.Point(8, 8);
			this.label1.Name = "label1";
			this.label1.TabIndex = 0;
			this.label1.Text = "&Name";
			this.label1.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			// 
			// label2
			// 
			this.label2.FlatStyle = System.Windows.Forms.FlatStyle.System;
			this.label2.Location = new System.Drawing.Point(8, 32);
			this.label2.Name = "label2";
			this.label2.TabIndex = 2;
			this.label2.Text = "&Target URL";
			this.label2.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			// 
			// targetBox
			// 
			this.targetBox.Location = new System.Drawing.Point(120, 32);
			this.targetBox.Name = "targetBox";
			this.targetBox.Size = new System.Drawing.Size(336, 20);
			this.targetBox.TabIndex = 3;
			this.targetBox.Text = "";
			this.targetBox.KeyDown += new System.Windows.Forms.KeyEventHandler(this.NewRedirectForm_KeyDown);
			// 
			// buttonOK
			// 
			this.buttonOK.DialogResult = System.Windows.Forms.DialogResult.OK;
			this.buttonOK.FlatStyle = System.Windows.Forms.FlatStyle.System;
			this.buttonOK.Location = new System.Drawing.Point(304, 56);
			this.buttonOK.Name = "buttonOK";
			this.buttonOK.TabIndex = 4;
			this.buttonOK.Text = "&OK";
			this.buttonOK.KeyDown += new System.Windows.Forms.KeyEventHandler(this.NewRedirectForm_KeyDown);
			// 
			// buttonCancel
			// 
			this.buttonCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.buttonCancel.FlatStyle = System.Windows.Forms.FlatStyle.System;
			this.buttonCancel.Location = new System.Drawing.Point(384, 56);
			this.buttonCancel.Name = "buttonCancel";
			this.buttonCancel.TabIndex = 5;
			this.buttonCancel.Text = "&Cancel";
			this.buttonCancel.KeyDown += new System.Windows.Forms.KeyEventHandler(this.NewRedirectForm_KeyDown);
			// 
			// NewRedirectForm
			// 
			this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
			this.ClientSize = new System.Drawing.Size(466, 87);
			this.Controls.Add(this.buttonCancel);
			this.Controls.Add(this.buttonOK);
			this.Controls.Add(this.targetBox);
			this.Controls.Add(this.nameBox);
			this.Controls.Add(this.label2);
			this.Controls.Add(this.label1);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "NewRedirectForm";
			this.Text = "Create Redirect";
			this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.NewRedirectForm_KeyDown);
			this.Closing += new System.ComponentModel.CancelEventHandler(this.NewRedirectForm_Closing);
			this.ResumeLayout(false);

		}
		#endregion

		private void NewRedirectForm_Closing(object sender, System.ComponentModel.CancelEventArgs e)
		{
			if(DialogResult != DialogResult.OK)
				return;

			if(nameBox.Text.Length == 0)
			{
				MessageBox.Show(this, "You must enter a name.");
				e.Cancel = true;
				nameBox.Select();
				return;
			}

			if(targetBox.Text.Length == 0)
			{
				MessageBox.Show(this, "You must enter a target URL.");
				e.Cancel = true;
				targetBox.Select();
				return;
			}
		}

		private void NewRedirectForm_KeyDown(object sender, System.Windows.Forms.KeyEventArgs e)
		{
			if(e.KeyCode == Keys.Enter)
			{
				DialogResult = DialogResult.OK;
				this.Close();
			}
			else if(e.KeyCode == Keys.Escape)
			{
				DialogResult = DialogResult.Cancel;
				this.Close();
			}
		}

		public string RedirectName
		{
			get
			{
				return nameBox.Text;
			}
		}

		public string RedirectTarget
		{
			get
			{
				return targetBox.Text;
			}
		}

	}
}
