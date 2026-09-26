namespace CC.winforms.Forms.Authentication
{
    partial class LoginFormNew
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(LoginFormNew));
            panel1 = new Panel();
            label1 = new Label();
            label2 = new Label();
            pictureBox1 = new PictureBox();
            label3 = new Label();
            label4 = new Label();
            label5 = new Label();
            label6 = new Label();
            label7 = new Label();
            panel2 = new Panel();
            textBox1 = new TextBox();
            panel3 = new Panel();
            label8 = new Label();
            textBox2 = new TextBox();
            button1 = new Button();
            panel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)pictureBox1).BeginInit();
            panel2.SuspendLayout();
            panel3.SuspendLayout();
            SuspendLayout();
            // 
            // panel1
            // 
            panel1.BackColor = Color.RosyBrown;
            panel1.Controls.Add(label4);
            panel1.Controls.Add(label3);
            panel1.Controls.Add(pictureBox1);
            panel1.Controls.Add(label2);
            panel1.Controls.Add(label1);
            panel1.Location = new Point(0, 0);
            panel1.Name = "panel1";
            panel1.Size = new Size(427, 612);
            panel1.TabIndex = 0;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Font = new Font("Georgia", 18F, FontStyle.Bold, GraphicsUnit.Point, 0);
            label1.ForeColor = SystemColors.ButtonHighlight;
            label1.Location = new Point(18, 276);
            label1.Name = "label1";
            label1.Size = new Size(406, 56);
            label1.TabIndex = 0;
            label1.Text = "Every cake tells";
            label1.Click += label1_Click;
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Font = new Font("Georgia", 18F, FontStyle.Bold, GraphicsUnit.Point, 0);
            label2.ForeColor = SystemColors.ButtonHighlight;
            label2.Location = new Point(18, 332);
            label2.Name = "label2";
            label2.Size = new Size(359, 56);
            label2.TabIndex = 1;
            label2.Text = "a sweet story.";
            label2.Click += label2_Click;
            // 
            // pictureBox1
            // 
            pictureBox1.Image = (Image)resources.GetObject("pictureBox1.Image");
            pictureBox1.Location = new Point(-9, -21);
            pictureBox1.Name = "pictureBox1";
            pictureBox1.Size = new Size(121, 110);
            pictureBox1.SizeMode = PictureBoxSizeMode.Zoom;
            pictureBox1.TabIndex = 1;
            pictureBox1.TabStop = false;
            pictureBox1.Click += pictureBox1_Click_1;
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Font = new Font("Georgia", 10.125F, FontStyle.Bold, GraphicsUnit.Point, 0);
            label3.ForeColor = SystemColors.ButtonHighlight;
            label3.Location = new Point(79, 9);
            label3.Name = "label3";
            label3.Size = new Size(288, 31);
            label3.TabIndex = 2;
            label3.Text = "Custom Cake CRMS";
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Font = new Font("Georgia", 7.875F, FontStyle.Regular, GraphicsUnit.Point, 0);
            label4.ForeColor = SystemColors.ButtonHighlight;
            label4.Location = new Point(79, 40);
            label4.Name = "label4";
            label4.Size = new Size(263, 25);
            label4.TabIndex = 3;
            label4.Text = "Crafted for cake businesses";
            // 
            // label5
            // 
            label5.AutoSize = true;
            label5.Font = new Font("Georgia", 22.125F, FontStyle.Bold, GraphicsUnit.Point, 0);
            label5.ForeColor = SystemColors.ActiveCaptionText;
            label5.Location = new Point(489, 76);
            label5.Name = "label5";
            label5.Size = new Size(475, 67);
            label5.TabIndex = 4;
            label5.Text = "Welcome Back";
            label5.Click += label5_Click;
            // 
            // label6
            // 
            label6.AutoSize = true;
            label6.Font = new Font("Georgia", 7.875F, FontStyle.Regular, GraphicsUnit.Point, 0);
            label6.ForeColor = SystemColors.ActiveCaptionText;
            label6.Location = new Point(505, 143);
            label6.Name = "label6";
            label6.Size = new Size(338, 25);
            label6.TabIndex = 4;
            label6.Text = "Sign in to your account to continue.";
            // 
            // label7
            // 
            label7.AutoSize = true;
            label7.Font = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point, 0);
            label7.ForeColor = SystemColors.ActiveCaptionText;
            label7.Location = new Point(505, 203);
            label7.Name = "label7";
            label7.Size = new Size(128, 32);
            label7.TabIndex = 5;
            label7.Text = "Username";
            // 
            // panel2
            // 
            panel2.Controls.Add(textBox1);
            panel2.Location = new Point(505, 238);
            panel2.Name = "panel2";
            panel2.Size = new Size(338, 36);
            panel2.TabIndex = 6;
            // 
            // textBox1
            // 
            textBox1.BorderStyle = BorderStyle.None;
            textBox1.Font = new Font("Segoe UI", 10.125F, FontStyle.Regular, GraphicsUnit.Point, 0);
            textBox1.Location = new Point(0, 0);
            textBox1.Name = "textBox1";
            textBox1.Size = new Size(338, 36);
            textBox1.TabIndex = 0;
            textBox1.TextChanged += textBox1_TextChanged_1;
            // 
            // panel3
            // 
            panel3.Controls.Add(textBox2);
            panel3.Location = new Point(505, 332);
            panel3.Name = "panel3";
            panel3.Size = new Size(338, 36);
            panel3.TabIndex = 8;
            // 
            // label8
            // 
            label8.AutoSize = true;
            label8.Font = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point, 0);
            label8.ForeColor = SystemColors.ActiveCaptionText;
            label8.Location = new Point(505, 297);
            label8.Name = "label8";
            label8.Size = new Size(122, 32);
            label8.TabIndex = 7;
            label8.Text = "Password";
            // 
            // textBox2
            // 
            textBox2.BorderStyle = BorderStyle.None;
            textBox2.Font = new Font("Segoe UI", 10.125F, FontStyle.Regular, GraphicsUnit.Point, 0);
            textBox2.Location = new Point(0, 0);
            textBox2.Name = "textBox2";
            textBox2.Size = new Size(338, 36);
            textBox2.TabIndex = 1;
            // 
            // button1
            // 
            button1.BackColor = Color.RosyBrown;
            button1.Location = new Point(505, 385);
            button1.Name = "button1";
            button1.Size = new Size(338, 46);
            button1.TabIndex = 9;
            button1.Text = "Sign Up";
            button1.UseVisualStyleBackColor = false;
            // 
            // LoginFormNew
            // 
            AutoScaleDimensions = new SizeF(192F, 192F);
            AutoScaleMode = AutoScaleMode.Dpi;
            ClientSize = new Size(1074, 609);
            Controls.Add(button1);
            Controls.Add(panel3);
            Controls.Add(label8);
            Controls.Add(panel2);
            Controls.Add(label7);
            Controls.Add(label6);
            Controls.Add(label5);
            Controls.Add(panel1);
            MinimumSize = new Size(900, 600);
            Name = "LoginFormNew";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "LoginFormNew";
            Load += LoginFormNew_Load;
            panel1.ResumeLayout(false);
            panel1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)pictureBox1).EndInit();
            panel2.ResumeLayout(false);
            panel2.PerformLayout();
            panel3.ResumeLayout(false);
            panel3.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Panel panel1;
        private Label label1;
        private Label label2;
        private PictureBox pictureBox1;
        private Label label4;
        private Label label3;
        private Label label5;
        private Label label6;
        private Label label7;
        private Panel panel2;
        private TextBox textBox1;
        private Panel panel3;
        private Label label8;
        private TextBox textBox2;
        private Button button1;
    }
}