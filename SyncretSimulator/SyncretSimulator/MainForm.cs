using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace SyncretSimulator
{
    public partial class MainForm : Form
    {
        // --- STARE SISTEM ---
        private bool M1, M2, M3, M4;
        private bool isAlarmActive = false;
        private int alarmTimerTicks = 0;

        // --- REGISTRE DE IMPULSURI PLC ---
        private bool pulseS0, pulseS1, pulseS2, pulseS3, pulseS4, pulseS5;

        // --- TRACKING STARE ANTERIOARĂ (evită UPSERT la fiecare tick dacă nu s-a schimbat nimic) ---
        private bool _prevM1, _prevM2, _prevM3, _prevM4, _prevAlarm;
        private string _prevClapeta = "None";

        // --- ALARMĂ SONORĂ ---
        private System.Threading.CancellationTokenSource _alarmCts;

        public MainForm()
        {
            InitializeComponent();
            this.Load += (s, e) =>
            {
                ApplyIndustrialStyle();
                mainTimer.Interval = 100;
                mainTimer.Enabled = true;
            };
        }

        // --- OB1 SCAN CYCLE ---
        private void mainTimer_Tick(object sender, EventArgs e)
        {
            ExecuteSafetyLogic();
            ExecuteProcessLogic();
            UpdateUI();
            pulseS1 = pulseS2 = pulseS3 = pulseS4 = pulseS5 = pulseS0 = false;
        }

        private void ExecuteSafetyLogic()
        {
            if (pulseS0)
            {
                StopAllMotors();
                LogAction("System", "EMERGENCY_STOP", "STOP GENERAL (S0)");
            }

            int activeSensors = (chkS6.Checked ? 1 : 0) + (chkS7.Checked ? 1 : 0) + (chkS8.Checked ? 1 : 0);
            if (activeSensors >= 2 && !isAlarmActive)
            {
                TriggerAlarm("EROARE: Conflict poziție clapetă!");
            }
        }

        private void ExecuteProcessLogic()
        {
            if (isAlarmActive)
            {
                alarmTimerTicks--;
                if (alarmTimerTicks <= 0) ResetAlarm();
                return;
            }

            if (pulseS3) { M3 = true; LogAction("B3", "MOTOR_START", "Start B3 (Ieșire)"); }
            if (pulseS4) { M4 = true; LogAction("B4", "MOTOR_START", "Start B4 (Ieșire)"); }

            bool pathB1Valid = (chkS6.Checked && M3) || chkS7.Checked;
            if (pulseS1)
            {
                if (pathB1Valid) LogAction("B1", "MOTOR_START", "Start B1");
                else LogAction("B1", "START_DENIED", "Eroare: Condiții pornire B1 neîndeplinite.");
            }
            M1 = (M1 || pulseS1) && pathB1Valid && !pulseS5;

            bool pathB2Valid = (chkS8.Checked && M4) || chkS7.Checked;
            if (pulseS2)
            {
                if (pathB2Valid) LogAction("B2", "MOTOR_START", "Start B2");
                else LogAction("B2", "START_DENIED", "Eroare: Condiții pornire B2 neîndeplinite.");
            }
            M2 = (M2 || pulseS2) && pathB2Valid && !pulseS5;
        }

        // --- BUTOANE ---
        private void btnS1_Click(object sender, EventArgs e) { if (!isAlarmActive) pulseS1 = true; }
        private void btnS2_Click(object sender, EventArgs e) { if (!isAlarmActive) pulseS2 = true; }
        private void btnS3_Click(object sender, EventArgs e) { if (!isAlarmActive) pulseS3 = true; }
        private void btnS4_Click(object sender, EventArgs e) { if (!isAlarmActive) pulseS4 = true; }
        private void btnS0_Click(object sender, EventArgs e) { pulseS0 = true; }
        private void btnS5_Click(object sender, EventArgs e)
        {
            if (!isAlarmActive)
            {
                pulseS5 = true;
                LogAction("B1_B2", "MOTOR_STOP", "Stop Intrări (S5)");
            }
        }

        // --- HELPERS ---
        private void StopAllMotors() { M1 = M2 = M3 = M4 = false; }

        private void TriggerAlarm(string message)
        {
            isAlarmActive = true;
            alarmTimerTicks = 50;
            StopAllMotors();
            lblStatus.Text = message;
            lblStatus.ForeColor = Color.Red;
            if (lblAlarm != null) lblAlarm.Visible = true;
            LogAction("Clapeta", "ALARM", "ALARMĂ: " + message);

            // Alarmă sonoră pe thread separat — nu blochează OB1
            _alarmCts?.Cancel();
            _alarmCts = new System.Threading.CancellationTokenSource();
            var token = _alarmCts.Token;

            System.Threading.Tasks.Task.Run(() =>
            {
                var endTime = DateTime.Now.AddSeconds(5);
                while (DateTime.Now < endTime && !token.IsCancellationRequested)
                {
                    Console.Beep(880, 200);
                    if (token.IsCancellationRequested) break;
                    System.Threading.Thread.Sleep(200);
                }
            }, token);
        }

        private void ResetAlarm()
        {
            // Oprește beep-ul imediat dacă mai rulează
            _alarmCts?.Cancel();

            isAlarmActive = false;
            lblStatus.Text = "SYNCRET SYSTEM READY";
            lblStatus.ForeColor = Color.White;
            if (lblAlarm != null) lblAlarm.Visible = false;
        }

        private string GetClapetaPos()
        {
            if (chkS6.Checked) return "S6";
            if (chkS7.Checked) return "S7";
            if (chkS8.Checked) return "S8";
            return "None";
        }

        // --- LOGARE ---
        private void LogAction(string component, string eventType, string message)
        {
            if (lstEvents != null)
                lstEvents.Items.Insert(0, $"[{DateTime.Now:HH:mm:ss}] {message}");

            Infrastructure.SqlLogger.LogAsync(component, eventType, message);
        }

        private void LogAction(string message) => LogAction("System", "INFO", message);

        // --- UPDATE UI + SYNC STARE ---
        private void UpdateUI()
        {
            // Culori benzi
            pnlB1.BackColor = M1 ? Color.LimeGreen : Color.FromArgb(60, 60, 60);
            pnlB2.BackColor = M2 ? Color.LimeGreen : Color.FromArgb(60, 60, 60);
            pnlB3.BackColor = M3 ? Color.LimeGreen : Color.FromArgb(60, 60, 60);
            pnlB4.BackColor = M4 ? Color.LimeGreen : Color.FromArgb(60, 60, 60);

            // LED-uri senzori
            if (isAlarmActive && alarmTimerTicks % 2 == 0)
            {
                pnlS6.BackColor = pnlS7.BackColor = pnlS8.BackColor = Color.Red;
            }
            else
            {
                pnlS6.BackColor = chkS6.Checked ? Color.Yellow : Color.DimGray;
                pnlS7.BackColor = chkS7.Checked ? Color.Yellow : Color.DimGray;
                pnlS8.BackColor = chkS8.Checked ? Color.Yellow : Color.DimGray;
            }

            // Sync ProcessState în DB — doar dacă ceva s-a schimbat față de tick-ul anterior
            string clapeta = GetClapetaPos();
            bool stateChanged = M1 != _prevM1 || M2 != _prevM2 ||
                                M3 != _prevM3 || M4 != _prevM4 ||
                                isAlarmActive != _prevAlarm ||
                                clapeta != _prevClapeta;

            if (stateChanged)
            {
                Infrastructure.SqlLogger.UpsertStateAsync(M1, M2, M3, M4, isAlarmActive, clapeta);
                _prevM1 = M1; _prevM2 = M2; _prevM3 = M3; _prevM4 = M4;
                _prevAlarm = isAlarmActive;
                _prevClapeta = clapeta;
            }
        }

        // --- STYLING ---
        private void ApplyIndustrialStyle()
        {
            this.BackColor = Color.FromArgb(30, 30, 30);
            StyleLargePanel(pnlB1); StyleLargePanel(pnlB2);
            StyleLargePanel(pnlB3); StyleLargePanel(pnlB4);
            StyleSensorLED(pnlS6); StyleSensorLED(pnlS7); StyleSensorLED(pnlS8);
            StyleButtonS0(btnS0);
            StyleButtonS1S4(btnS1); StyleButtonS1S4(btnS2);
            StyleButtonS1S4(btnS3); StyleButtonS1S4(btnS4);
            StyleButtonS1S4(btnS5);
            StyleStatusLabel(lblStatus);
            btnS5.BackColor = Color.DarkOrange;
            CheckBox[] sensors = { chkS6, chkS7, chkS8 };
            foreach (var chk in sensors)
            {
                chk.ForeColor = Color.White;
                chk.Font = new Font("Segoe UI", 9, FontStyle.Bold);
                chk.BackColor = Color.Transparent;
            }
            SenzoriClapeta.ForeColor = Color.LightGray;
        }

        private void StyleLargePanel(Panel p) { p.BackColor = Color.FromArgb(50, 50, 50); p.BorderStyle = BorderStyle.FixedSingle; }
        private void StyleSensorLED(Panel p)
        {
            GraphicsPath path = new GraphicsPath();
            path.AddEllipse(0, 0, p.Width, p.Height);
            p.Region = new Region(path);
        }
        private void StyleButtonS0(Button b)
        {
            b.BackColor = Color.Crimson; b.FlatStyle = FlatStyle.Flat;
            b.Font = new Font("Segoe UI", 10, FontStyle.Bold); b.ForeColor = Color.White;
            GraphicsPath path = new GraphicsPath();
            path.AddEllipse(0, 0, b.Width, b.Height);
            b.Region = new Region(path);
        }
        private void StyleButtonS1S4(Button b) { b.BackColor = Color.FromArgb(50, 150, 50); b.FlatStyle = FlatStyle.Flat; b.ForeColor = Color.White; b.Font = new Font("Segoe UI", 9, FontStyle.Bold); }
        private void StyleStatusLabel(Label l) { l.Dock = DockStyle.Top; l.Height = 50; l.TextAlign = ContentAlignment.MiddleCenter; l.ForeColor = Color.White; l.Font = new Font("Segoe UI", 14, FontStyle.Bold); }

        private void pnlS6_Paint(object sender, PaintEventArgs e) { }
        private void labelB2_Click(object sender, EventArgs e) { }
    }
}