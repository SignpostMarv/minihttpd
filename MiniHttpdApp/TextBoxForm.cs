using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;

namespace MiniHttpdApp
{
	/// <summary>
	/// Summary description for TextBoxForm.
	/// </summary>
	public class TextBoxForm : System.Windows.Forms.Form
	{
		private System.Windows.Forms.TextBox textBox;
		private System.Windows.Forms.Button button1;
		private System.Windows.Forms.Button buttonOK;
		private System.Windows.Forms.Label caption;
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;

		public TextBoxForm()
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
			this.textBox = new System.Windows.Forms.TextBox();
			this.button1 = new System.Windows.Forms.Button();
			this.buttonOK = new System.Windows.Forms.Button();
			this.caption = new System.Windows.Forms.Label();
			this.SuspendLayout();
			// 
			// textBox
			// 
			this.textBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
				| System.Windows.Forms.AnchorStyles.Right)));
			this.textBox.Location = new System.Drawing.Point(8, 32);
			this.textBox.Name = "textBox";
			this.textBox.Size = new System.Drawing.Size(344, 20);
			this.textBox.TabIndex = 0;
			this.textBox.Text = "";
			this.textBox.KeyDown += new System.Windows.Forms.KeyEventHandler(this.TextBoxForm_KeyDown);
			// 
			// button1
			// 
			this.button1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.button1.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.button1.FlatStyle = System.Windows.Forms.FlatStyle.System;
			this.button1.Location = new System.Drawing.Point(280, 58);
			this.button1.Name = "button1";
			this.button1.TabIndex = 2;
			this.button1.Text = "&Cancel";
			this.button1.KeyDown += new System.Windows.Forms.KeyEventHandler(this.TextBoxForm_KeyDown);
			// 
			// buttonOK
			// 
			this.buttonOK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.buttonOK.DialogResult = System.Windows.Forms.DialogResult.OK;
			this.buttonOK.FlatStyle = System.Windows.Forms.FlatStyle.System;
			this.buttonOK.Location = new System.Drawing.Point(200, 58);
			this.buttonOK.Name = "buttonOK";
			this.buttonOK.TabIndex = 1;
			this.buttonOK.Text = "&OK";
			this.buttonOK.KeyDown += new System.Windows.Forms.KeyEventHandler(this.TextBoxForm_KeyDown);
			// 
			// caption
			// 
			this.caption.FlatStyle = System.Windows.Forms.FlatStyle.System;
			this.caption.Location = new System.Drawing.Point(8, 8);
			this.caption.Name = "caption";
			this.caption.Size = new System.Drawing.Size(344, 23);
			this.caption.TabIndex = 3;
			// 
			// TextBoxForm
			// 
			this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
			this.ClientSize = new System.Drawing.Size(360, 85);
			this.ControlBox = false;
			this.Controls.Add(this.caption);
			this.Controls.Add(this.buttonOK);
			this.Controls.Add(this.button1);
			this.Controls.Add(this.textBox);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
			this.Name = "TextBoxForm";
			this.ShowInTaskbar = false;
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Text = "TextBoxForm";
			this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.TextBoxForm_KeyDown);
			this.Closing += new System.ComponentModel.CancelEventHandler(this.TextBoxForm_Closing);
			this.ResumeLayout(false);

		}
		#endregion

		private void TextBoxForm_KeyDown(object sender, System.Windows.Forms.KeyEventArgs e)
		{
			if(e.KeyCode == Keys.Enter)
			{
				this.DialogResult = DialogResult.OK;
				this.Close();
			}
			else if(e.KeyCode == Keys.Escape)
			{
				this.DialogResult = DialogResult.Cancel;
				this.Close();
			}
		}

		private void TextBoxForm_Closing(object sender, System.ComponentModel.CancelEventArgs e)
		{
			if(this.DialogResult == DialogResult.OK)
			{
				if(this.TextBox.Text == null || this.TextBox.Text == string.Empty)
				{
					MessageBox.Show(this, "You must enter a value.");
					this.TextBox.SelectAll();
					e.Cancel = true;
					return;
				}
			}
		}

		public TextBox TextBox
		{
			get
			{
				return this.textBox;
			}
		}

		public string Caption
		{
			get
			{
				return this.caption.Text;
			}
			set
			{
				caption.Text = value;
			}
		}
	}
}
