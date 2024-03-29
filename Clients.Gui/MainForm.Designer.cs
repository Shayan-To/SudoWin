/*
Copyright (c) 2005-2008, Schley Andrew Kutz <akutz@lostcreations.com>
All rights reserved.

Redistribution and use in source and binary forms, with or without modification,
are permitted provided that the following conditions are met:

	* Redistributions of source code must retain the above copyright notice,
	this list of conditions and the following disclaimer.
	* Redistributions in binary form must reproduce the above copyright notice,
	this list of conditions and the following disclaimer in the documentation
	and/or other materials provided with the distribution.
	* Neither the name of l o s t c r e a t i o n s nor the names of its 
	contributors may be used to endorse or promote products derived from this 
	software without specific prior written permission.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND
ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE LIABLE FOR
ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
(INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON
ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
(INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
*/

namespace Sudowin.Clients.Gui
{
	partial class MainForm 
	{
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose( bool disposing )
		{
			if ( disposing && ( components != null ) )
			{
				components.Dispose();
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
			this.m_btn_ok = new System.Windows.Forms.Button();
			this.m_btn_cancel = new System.Windows.Forms.Button();
			this.m_txtbox_password = new System.Windows.Forms.TextBox();
			this.m_picbox_user_icon = new System.Windows.Forms.PictureBox();
			this.m_picbox_sudoed_cmd = new System.Windows.Forms.PictureBox();
			this.m_lbl_sudoed_cmd = new System.Windows.Forms.Label();
			this.label2 = new System.Windows.Forms.Label();
			this.m_lbl_warning = new System.Windows.Forms.Label();
			((System.ComponentModel.ISupportInitialize)(this.m_picbox_user_icon)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.m_picbox_sudoed_cmd)).BeginInit();
			this.SuspendLayout();
			// 
			// m_btn_ok
			// 
			this.m_btn_ok.Location = new System.Drawing.Point(71, 97);
			this.m_btn_ok.Name = "m_btn_ok";
			this.m_btn_ok.Size = new System.Drawing.Size(75, 23);
			this.m_btn_ok.TabIndex = 1;
			this.m_btn_ok.Text = "&Ok";
			this.m_btn_ok.Click += new System.EventHandler(this.btnOk_Click);
			// 
			// m_btn_cancel
			// 
			this.m_btn_cancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.m_btn_cancel.Location = new System.Drawing.Point(152, 97);
			this.m_btn_cancel.Name = "m_btn_cancel";
			this.m_btn_cancel.Size = new System.Drawing.Size(75, 23);
			this.m_btn_cancel.TabIndex = 2;
			this.m_btn_cancel.Text = "&Cancel";
			this.m_btn_cancel.Click += new System.EventHandler(this.m_btn_cancel_Click);
			// 
			// m_txtbox_password
			// 
			this.m_txtbox_password.Location = new System.Drawing.Point(71, 25);
			this.m_txtbox_password.Name = "m_txtbox_password";
			this.m_txtbox_password.PasswordChar = '●';
			this.m_txtbox_password.Size = new System.Drawing.Size(156, 20);
			this.m_txtbox_password.TabIndex = 0;
			this.m_txtbox_password.UseSystemPasswordChar = true;
			// 
			// m_picbox_user_icon
			// 
			this.m_picbox_user_icon.BackColor = System.Drawing.Color.White;
			this.m_picbox_user_icon.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Center;
			this.m_picbox_user_icon.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
			this.m_picbox_user_icon.Location = new System.Drawing.Point(7, 9);
			this.m_picbox_user_icon.Name = "m_picbox_user_icon";
			this.m_picbox_user_icon.Size = new System.Drawing.Size(55, 55);
			this.m_picbox_user_icon.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
			this.m_picbox_user_icon.TabIndex = 3;
			this.m_picbox_user_icon.TabStop = false;
			// 
			// m_picbox_sudoed_cmd
			// 
			this.m_picbox_sudoed_cmd.Location = new System.Drawing.Point(71, 67);
			this.m_picbox_sudoed_cmd.Name = "m_picbox_sudoed_cmd";
			this.m_picbox_sudoed_cmd.Size = new System.Drawing.Size(24, 24);
			this.m_picbox_sudoed_cmd.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
			this.m_picbox_sudoed_cmd.TabIndex = 4;
			this.m_picbox_sudoed_cmd.TabStop = false;
			// 
			// m_lbl_sudoed_cmd
			// 
			this.m_lbl_sudoed_cmd.AutoSize = true;
			this.m_lbl_sudoed_cmd.Location = new System.Drawing.Point(101, 73);
			this.m_lbl_sudoed_cmd.Name = "m_lbl_sudoed_cmd";
			this.m_lbl_sudoed_cmd.Size = new System.Drawing.Size(0, 13);
			this.m_lbl_sudoed_cmd.TabIndex = 5;
			// 
			// label2
			// 
			this.label2.AutoSize = true;
			this.label2.Location = new System.Drawing.Point(68, 9);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(137, 13);
			this.label2.TabIndex = 6;
			this.label2.Text = "Please enter your password";
			// 
			// m_lbl_warning
			// 
			this.m_lbl_warning.AutoSize = true;
			this.m_lbl_warning.ForeColor = System.Drawing.Color.Red;
			this.m_lbl_warning.Location = new System.Drawing.Point(71, 50);
			this.m_lbl_warning.Name = "m_lbl_warning";
			this.m_lbl_warning.Size = new System.Drawing.Size(0, 13);
			this.m_lbl_warning.TabIndex = 7;
			// 
			// MainForm
			// 
			this.AcceptButton = this.m_btn_ok;
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.m_btn_cancel;
			this.ClientSize = new System.Drawing.Size(234, 123);
			this.ControlBox = false;
			this.Controls.Add(this.m_lbl_warning);
			this.Controls.Add(this.label2);
			this.Controls.Add(this.m_lbl_sudoed_cmd);
			this.Controls.Add(this.m_picbox_sudoed_cmd);
			this.Controls.Add(this.m_picbox_user_icon);
			this.Controls.Add(this.m_txtbox_password);
			this.Controls.Add(this.m_btn_cancel);
			this.Controls.Add(this.m_btn_ok);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.KeyPreview = true;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "MainForm";
			this.ShowIcon = false;
			this.ShowInTaskbar = false;
			this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
			this.Text = "Sudo for Windows";
			this.TopMost = true;
			((System.ComponentModel.ISupportInitialize)(this.m_picbox_user_icon)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.m_picbox_sudoed_cmd)).EndInit();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Button m_btn_ok;
		private System.Windows.Forms.Button m_btn_cancel;
		private System.Windows.Forms.TextBox m_txtbox_password;
		private System.Windows.Forms.PictureBox m_picbox_user_icon;
		private System.Windows.Forms.PictureBox m_picbox_sudoed_cmd;
		private System.Windows.Forms.Label m_lbl_sudoed_cmd;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.Label m_lbl_warning;

	}
}