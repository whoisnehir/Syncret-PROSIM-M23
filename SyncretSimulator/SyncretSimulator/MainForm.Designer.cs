namespace SyncretSimulator
{
    partial class MainForm
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.pnlB1 = new System.Windows.Forms.Panel();
            this.pnlB2 = new System.Windows.Forms.Panel();
            this.pnlB3 = new System.Windows.Forms.Panel();
            this.pnlB4 = new System.Windows.Forms.Panel();
            this.chkS6 = new System.Windows.Forms.CheckBox();
            this.chkS7 = new System.Windows.Forms.CheckBox();
            this.chkS8 = new System.Windows.Forms.CheckBox();
            this.btnS0 = new System.Windows.Forms.Button();
            this.btnS1 = new System.Windows.Forms.Button();
            this.btnS2 = new System.Windows.Forms.Button();
            this.btnS3 = new System.Windows.Forms.Button();
            this.btnS4 = new System.Windows.Forms.Button();
            this.btnS5 = new System.Windows.Forms.Button();
            this.lblAlarm = new System.Windows.Forms.Label();
            this.lstEvents = new System.Windows.Forms.ListBox();
            this.mainTimer = new System.Windows.Forms.Timer(this.components);
            this.labelB1 = new System.Windows.Forms.Label();
            this.labelB2 = new System.Windows.Forms.Label();
            this.labelB3 = new System.Windows.Forms.Label();
            this.labelB4 = new System.Windows.Forms.Label();
            this.SenzoriClapeta = new System.Windows.Forms.GroupBox();
            this.pnlS6 = new System.Windows.Forms.Panel();
            this.pnlS7 = new System.Windows.Forms.Panel();
            this.pnlS8 = new System.Windows.Forms.Panel();
            this.lblStatus = new System.Windows.Forms.Label();
            this.SenzoriClapeta.SuspendLayout();
            this.SuspendLayout();
            // 
            // pnlB1, pnlB2, pnlB3, pnlB4 (Configurări locații)
            // 
            this.pnlB1.Location = new System.Drawing.Point(193, 226);
            this.pnlB1.Name = "pnlB1";
            this.pnlB1.Size = new System.Drawing.Size(150, 30);

            this.pnlB2.Location = new System.Drawing.Point(508, 226);
            this.pnlB2.Name = "pnlB2";
            this.pnlB2.Size = new System.Drawing.Size(150, 30);

            this.pnlB3.Location = new System.Drawing.Point(199, 542);
            this.pnlB3.Name = "pnlB3";
            this.pnlB3.Size = new System.Drawing.Size(150, 30);

            this.pnlB4.Location = new System.Drawing.Point(508, 542);
            this.pnlB4.Name = "pnlB4";
            this.pnlB4.Size = new System.Drawing.Size(150, 30);

            // 
            // Butoane (S0 - S5)
            // 
            this.btnS0.Location = new System.Drawing.Point(22, 134);
            this.btnS0.Name = "btnS0";
            this.btnS0.Size = new System.Drawing.Size(60, 60);
            this.btnS0.Text = "S0";
            this.btnS0.Click += new System.EventHandler(this.btnS0_Click);

            this.btnS1.Location = new System.Drawing.Point(22, 226);
            this.btnS1.Name = "btnS1";
            this.btnS1.Size = new System.Drawing.Size(50, 50);
            this.btnS1.Text = "S1";
            this.btnS1.Click += new System.EventHandler(this.btnS1_Click);

            this.btnS2.Location = new System.Drawing.Point(22, 322);
            this.btnS2.Name = "btnS2";
            this.btnS2.Size = new System.Drawing.Size(50, 50);
            this.btnS2.Text = "S2";
            this.btnS2.Click += new System.EventHandler(this.btnS2_Click);

            this.btnS3.Location = new System.Drawing.Point(22, 422);
            this.btnS3.Name = "btnS3";
            this.btnS3.Size = new System.Drawing.Size(50, 50);
            this.btnS3.Text = "S3";
            this.btnS3.Click += new System.EventHandler(this.btnS3_Click);

            this.btnS4.Location = new System.Drawing.Point(22, 528);
            this.btnS4.Name = "btnS4";
            this.btnS4.Size = new System.Drawing.Size(50, 50);
            this.btnS4.Text = "S4";
            this.btnS4.Click += new System.EventHandler(this.btnS4_Click);

            this.btnS5.Location = new System.Drawing.Point(22, 626);
            this.btnS5.Name = "btnS5";
            this.btnS5.Size = new System.Drawing.Size(50, 50);
            this.btnS5.Text = "S5";
            this.btnS5.Click += new System.EventHandler(this.btnS5_Click);

            // 
            // Senzori și Log
            // 
            this.chkS6.Location = new System.Drawing.Point(20, 30);
            this.chkS6.Name = "chkS6";
            this.chkS6.Text = "S6 (L)";

            this.chkS7.Location = new System.Drawing.Point(20, 60);
            this.chkS7.Name = "chkS7";
            this.chkS7.Text = "S7 (M)";

            this.chkS8.Location = new System.Drawing.Point(20, 90);
            this.chkS8.Name = "chkS8";
            this.chkS8.Text = "S8 (R)";

            this.lstEvents.Location = new System.Drawing.Point(772, 94);
            this.lstEvents.Name = "lstEvents";
            this.lstEvents.Size = new System.Drawing.Size(200, 400);

            this.mainTimer.Enabled = true;
            this.mainTimer.Interval = 100;
            this.mainTimer.Tick += new System.EventHandler(this.mainTimer_Tick);

            // LED-urile (Panelurile mici)
            this.pnlS6.Location = new System.Drawing.Point(199, 385);
            this.pnlS6.Size = new System.Drawing.Size(30, 30);
            this.pnlS6.Name = "pnlS6";

            this.pnlS7.Location = new System.Drawing.Point(401, 385);
            this.pnlS7.Size = new System.Drawing.Size(30, 30);
            this.pnlS7.Name = "pnlS7";

            this.pnlS8.Location = new System.Drawing.Point(606, 385);
            this.pnlS8.Size = new System.Drawing.Size(30, 30);
            this.pnlS8.Name = "pnlS8";

            this.lblStatus.Location = new System.Drawing.Point(400, 20);
            this.lblStatus.Name = "lblStatus";
            this.lblStatus.Text = "SYNCRET SYSTEM READY";
            this.lblStatus.ForeColor = System.Drawing.Color.White;

            // 
            // Adăugarea pe Form
            // 
            this.SenzoriClapeta.Location = new System.Drawing.Point(772, 551);
            this.SenzoriClapeta.Size = new System.Drawing.Size(150, 130);
            this.SenzoriClapeta.Controls.Add(this.chkS6);
            this.SenzoriClapeta.Controls.Add(this.chkS7);
            this.SenzoriClapeta.Controls.Add(this.chkS8);

            this.Controls.Add(this.lblStatus);
            this.Controls.Add(this.pnlB1);
            this.Controls.Add(this.pnlB2);
            this.Controls.Add(this.pnlB3);
            this.Controls.Add(this.pnlB4);
            this.Controls.Add(this.pnlS6);
            this.Controls.Add(this.pnlS7);
            this.Controls.Add(this.pnlS8);
            this.Controls.Add(this.btnS0);
            this.Controls.Add(this.btnS1);
            this.Controls.Add(this.btnS2);
            this.Controls.Add(this.btnS3);
            this.Controls.Add(this.btnS4);
            this.Controls.Add(this.btnS5);
            this.Controls.Add(this.lstEvents);
            this.Controls.Add(this.SenzoriClapeta);

            this.ClientSize = new System.Drawing.Size(984, 961);
            this.Name = "MainForm";
            this.Text = "Syncret M23 Simulator";
            this.SenzoriClapeta.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion

        // DECLARAȚIILE (Sunt vitale!)
        private System.Windows.Forms.Panel pnlB1;
        private System.Windows.Forms.Panel pnlB2;
        private System.Windows.Forms.Panel pnlB3;
        private System.Windows.Forms.Panel pnlB4;
        private System.Windows.Forms.CheckBox chkS6;
        private System.Windows.Forms.CheckBox chkS7;
        private System.Windows.Forms.CheckBox chkS8;
        private System.Windows.Forms.Button btnS0;
        private System.Windows.Forms.Button btnS1;
        private System.Windows.Forms.Button btnS2;
        private System.Windows.Forms.Button btnS3;
        private System.Windows.Forms.Button btnS4;
        private System.Windows.Forms.Button btnS5;
        private System.Windows.Forms.Label lblAlarm;
        private System.Windows.Forms.ListBox lstEvents;
        private System.Windows.Forms.Timer mainTimer;
        private System.Windows.Forms.GroupBox SenzoriClapeta;
        private System.Windows.Forms.Panel pnlS6;
        private System.Windows.Forms.Panel pnlS7;
        private System.Windows.Forms.Panel pnlS8;
        private System.Windows.Forms.Label lblStatus;
        private System.Windows.Forms.Label labelB1;
        private System.Windows.Forms.Label labelB2;
        private System.Windows.Forms.Label labelB3;
        private System.Windows.Forms.Label labelB4;
    }
}