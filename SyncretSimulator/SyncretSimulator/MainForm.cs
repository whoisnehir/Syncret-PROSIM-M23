using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace SyncretSimulator
{
    public partial class MainForm : Form
    {
        // --- STARE SISTEM (Variabile de proces / Ieșiri %Q) ---
        private bool M1, M2, M3, M4; // Motoare Benzi
        private bool isAlarmActive = false;
        private int alarmTimerTicks = 0; // Pentru cele 5 secunde (50 tick-uri la 100ms)

        // --- ADĂUGAT: REGISTRE DE IMPULSURI PLC (Intrări %I de tip Push-Button) ---
        // Simulează butoanele fizice care trimit semnal doar în momentul apăsării
        private bool pulseS0; // Stop General
        private bool pulseS1; // Start B1
        private bool pulseS2; // Start B2
        private bool pulseS3; // Start B3
        private bool pulseS4; // Start B4
        private bool pulseS5; // Stop Intrări

        public MainForm()
        {
            InitializeComponent();
            // Subscriere la evenimentul Load pentru a aplica stilul o singură dată
            this.Load += (s, e) =>
            {
                ApplyIndustrialStyle();
                // Configure timer after all controls are initialized
                mainTimer.Interval = 100;
                mainTimer.Enabled = true;
            };
        }

        // --- LOGICA DE CONTROL PLC (Executată ciclic la fiecare 100ms - OB1) ---
        private void mainTimer_Tick(object sender, EventArgs e)
        {
            ExecuteSafetyLogic();
            ExecuteProcessLogic();
            UpdateUI();

            // --- ADĂUGAT: RESETARE PULSURI LA FINAL DE CICLU ---
            // După ce PLC-ul a procesat logica, considerăm că operatorul a luat mâna de pe butoane
            pulseS1 = pulseS2 = pulseS3 = pulseS4 = pulseS5 = pulseS0 = false;
        }

        private void ExecuteSafetyLogic()
        {
            // Tratare puls Stop General (S0) în interiorul ciclului de scanare
            if (pulseS0)
            {
                StopAllMotors();
                LogAction("System", "EMERGENCY_STOP", "STOP GENERAL (S0)");
            }

            // 1. Verificare conflict senzori clapetă (S6, S7, S8)
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
                if (alarmTimerTicks <= 0)
                {
                    ResetAlarm();
                }
                return;
            }

            // --- LOGICĂ REȚELE IN STIL LADDER SIEMENS (Automenținere) ---

            // Comenzi pentru pornire benzi ieșire B3 și B4 (Menținere implicită)
            if (pulseS3) { M3 = true; LogAction("B3", "MOTOR_START", "Start B3 (Ieșire)"); }
            if (pulseS4) { M4 = true; LogAction("B4", "MOTOR_START", "Start B4 (Ieșire)"); }

            // Regula B1: Condiție validare rută industrială
            bool pathB1Valid = (chkS6.Checked && M3) || chkS7.Checked;

            // Evaluare feedback la apăsare buton
            if (pulseS1)
            {
                if (pathB1Valid)
                    LogAction("B1", "MOTOR_START", "Start B1");
                else
                    LogAction("B1", "START_DENIED", "Eroare: Condiții pornire B1 neîndeplinite.");
            }

            // Ecuația Booleană de Automenținere PLC pentru M1:
            // (A fost apăsat StartB1 SAU mergea deja M1) ȘI ruta e validă ȘI NU s-a apăsat Stop Intrări (S5)
            M1 = (M1 || pulseS1) && pathB1Valid && !pulseS5;

            // Regula B2: Simetrică pentru banda 2
            bool pathB2Valid = (chkS8.Checked && M4) || chkS7.Checked;

            if (pulseS2)
            {
                if (pathB2Valid)
                    LogAction("B2", "MOTOR_START", "Start B2");
                else
                    LogAction("B2", "START_DENIED", "Eroare: Condiții pornire B2 neîndeplinite.");
            }

            // Ecuația Booleană de Automenținere PLC pentru M2:
            M2 = (M2 || pulseS2) && pathB2Valid && !pulseS5;
        }

        // --- EVENIMENTE BUTOANE (Trimit doar impulsuri în memoria PLC) ---
        private void btnS1_Click(object sender, EventArgs e)
        {
            if (!isAlarmActive) pulseS1 = true;
        }

        private void btnS2_Click(object sender, EventArgs e)
        {
            if (!isAlarmActive) pulseS2 = true;
        }

        private void btnS3_Click(object sender, EventArgs e)
        {
            if (!isAlarmActive) pulseS3 = true;
        }

        private void btnS4_Click(object sender, EventArgs e)
        {
            if (!isAlarmActive) pulseS4 = true;
        }

        private void btnS0_Click(object sender, EventArgs e) // STOP GENERAL
        {
            pulseS0 = true;
        }

        private void btnS5_Click(object sender, EventArgs e) // STOP INTRĂRI
        {
            if (!isAlarmActive)
            {
                pulseS5 = true;
                LogAction("B1_B2", "MOTOR_STOP", "Stop Intrări (S5)");
            }
        }

        // --- METODE HELPER ---
        private void StopAllMotors()
        {
            M1 = M2 = M3 = M4 = false;
        }

        private void TriggerAlarm(string message)
        {
            isAlarmActive = true;
            alarmTimerTicks = 50; // 5 secunde
            StopAllMotors();
            lblStatus.Text = message;
            lblStatus.ForeColor = Color.Red;
            if (lblAlarm != null) lblAlarm.Visible = true;
            LogAction("Clapeta", "ALARM", "ALARMĂ: " + message);
        }

        private void ResetAlarm()
        {
            isAlarmActive = false;
            lblStatus.Text = "SYNCRET SYSTEM READY";
            lblStatus.ForeColor = Color.White;
            if (lblAlarm != null) lblAlarm.Visible = false;
        }

        // --- LOGARE EVENIMENTE ---

        // Overload complet: UI instant + DB asincron
        private void LogAction(string component, string eventType, string message)
        {
            if (lstEvents != null)
                lstEvents.Items.Insert(0, $"[{DateTime.Now:HH:mm:ss}] {message}");

            Infrastructure.SqlLogger.LogAsync(component, eventType, message);
        }

        // Overload de compatibilitate (fallback - pastreza apelurile vechi functionale)
        private void LogAction(string message)
            => LogAction("System", "INFO", message);

        private void UpdateUI()
        {
            // Actualizare culori benzi (Verde dacă merg, Gri dacă sunt oprite)
            pnlB1.BackColor = M1 ? Color.LimeGreen : Color.FromArgb(60, 60, 60);
            pnlB2.BackColor = M2 ? Color.LimeGreen : Color.FromArgb(60, 60, 60);
            pnlB3.BackColor = M3 ? Color.LimeGreen : Color.FromArgb(60, 60, 60);
            pnlB4.BackColor = M4 ? Color.LimeGreen : Color.FromArgb(60, 60, 60);

            // Actualizare LED-uri senzori (Galben dacă e bifat, Gri dacă nu)
            // Dacă e alarmă, facem "blink" cu roșu
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
        }

        // --- UI STYLING (Syncret Branding) ---
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
            b.BackColor = Color.Crimson; b.FlatStyle = FlatStyle.Flat; b.Font = new Font("Segoe UI", 10, FontStyle.Bold); b.ForeColor = Color.White;
            GraphicsPath path = new GraphicsPath(); path.AddEllipse(0, 0, b.Width, b.Height); b.Region = new Region(path);
        }
        private void StyleButtonS1S4(Button b) { b.BackColor = Color.FromArgb(50, 150, 50); b.FlatStyle = FlatStyle.Flat; b.ForeColor = Color.White; b.Font = new Font("Segoe UI", 9, FontStyle.Bold); }
        private void StyleStatusLabel(Label l) { l.Dock = DockStyle.Top; l.Height = 50; l.TextAlign = ContentAlignment.MiddleCenter; l.ForeColor = Color.White; l.Font = new Font("Segoe UI", 14, FontStyle.Bold); }

        private void pnlS6_Paint(object sender, PaintEventArgs e)
        {
            // Păstrat complet intact
        }

        private void labelB2_Click(object sender, EventArgs e)
        {

        }
    }
}