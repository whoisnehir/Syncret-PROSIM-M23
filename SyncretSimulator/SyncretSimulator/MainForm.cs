using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace SyncretSimulator
{
    public partial class MainForm : Form
    {
        // --- STARE SISTEM (Variabile de proces) ---
        private bool M1, M2, M3, M4; // Motoare Benzi
        private bool isAlarmActive = false;
        private int alarmTimerTicks = 0; // Pentru cele 5 secunde (50 tick-uri la 100ms)

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

        // --- LOGICA DE CONTROL (Executată la fiecare 100ms) ---
        private void mainTimer_Tick(object sender, EventArgs e)
        {
            ExecuteSafetyLogic();
            ExecuteProcessLogic();
            UpdateUI();
        }

        private void ExecuteSafetyLogic()
        {
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

            // --- REGULI LOGICE M23 (Interblocări) ---

            // Regula B1: Pornește/Rămâne pornită doar dacă:
            // (E pe ruta stânga S6 ȘI M3 merge) SAU (E pe ruta mijloc S7)
            if (M1)
            {
                bool pathValid = (chkS6.Checked && M3) || chkS7.Checked;
                if (!pathValid)
                {
                    M1 = false;
                    LogAction("B1 Oprită: Rută invalidă.");
                }
            }

            // Regula B2: Pornește/Rămâne pornită doar dacă:
            // (E pe ruta dreapta S8 ȘI M4 merge) SAU (E pe ruta mijloc S7)
            if (M2)
            {
                bool pathValid = (chkS8.Checked && M4) || chkS7.Checked;
                if (!pathValid)
                {
                    M2 = false;
                    LogAction("B2 Oprită: Rută invalidă.");
                }
            }

            // Notă: M3 și M4 (ieșirile) nu au interblocări în documentație, 
            // pot fi pornite/oprite liber de operator.
        }

        // --- EVENIMENTE BUTOANE (Comenzi Operator) ---
        private void btnS1_Click(object sender, EventArgs e)
        {
            if (isAlarmActive) return;
            // Verificăm condiția chiar la apăsare pentru feedback instant
            if ((chkS6.Checked && M3) || chkS7.Checked) { M1 = true; LogAction("Start B1"); }
            else { LogAction("Eroare: Condiții pornire B1 neîndeplinite."); }
        }

        private void btnS2_Click(object sender, EventArgs e)
        {
            if (isAlarmActive) return;
            if ((chkS8.Checked && M4) || chkS7.Checked) { M2 = true; LogAction("Start B2"); }
            else { LogAction("Eroare: Condiții pornire B2 neîndeplinite."); }
        }

        private void btnS3_Click(object sender, EventArgs e) { if (!isAlarmActive) { M3 = true; LogAction("Start B3 (Ieșire)"); } }
        private void btnS4_Click(object sender, EventArgs e) { if (!isAlarmActive) { M4 = true; LogAction("Start B4 (Ieșire)"); } }

        private void btnS0_Click(object sender, EventArgs e) // STOP GENERAL
        {
            StopAllMotors();
            LogAction("!!! STOP GENERAL (S0) !!!");
        }

        private void btnS5_Click(object sender, EventArgs e) // STOP INTRĂRI
        {
            M1 = false;
            M2 = false;
            LogAction("Stop Intrări (S5)");
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
            LogAction("ALARMĂ: " + message);
        }

        private void ResetAlarm()
        {
            isAlarmActive = false;
            lblStatus.Text = "SYSTEM READY";
            lblStatus.ForeColor = Color.White;
        }

        private void LogAction(string message)
        {
            if (lstEvents != null)
                lstEvents.Items.Insert(0, $"[{DateTime.Now:HH:mm:ss}] {message}");
        }

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
            StyleButtonS1S4(btnS5); // Am adăugat și S5
            StyleStatusLabel(lblStatus);
            btnS5.BackColor = Color.DarkOrange; // S5 e stop parțial, îl facem portocaliu
            CheckBox[] sensors = { chkS6, chkS7, chkS8 };
            foreach (var chk in sensors)
            {
                chk.ForeColor = Color.White;
                chk.Font = new Font("Segoe UI", 9, FontStyle.Bold);
                // Dacă fundalul GroupBox-ului e prea închis:
                chk.BackColor = Color.Transparent;
            }

            // Opțional: stilizează și GroupBox-ul care le conține
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
            b.BackColor = Color.Crimson; b.FlatStyle = FlatStyle.Flat; b.Font = new Font("Segoe UI", 10, FontStyle.Bold);
            GraphicsPath path = new GraphicsPath(); path.AddEllipse(0, 0, b.Width, b.Height); b.Region = new Region(path);
        }
        private void StyleButtonS1S4(Button b) { b.BackColor = Color.FromArgb(50, 150, 50); b.FlatStyle = FlatStyle.Flat; b.ForeColor = Color.White; }
        private void StyleStatusLabel(Label l) { l.Dock = DockStyle.Top; l.Height = 50; l.TextAlign = ContentAlignment.MiddleCenter; l.ForeColor = Color.White; }
        private void pnlS6_Paint(object sender, PaintEventArgs e)
        {
            // Nu scrie nimic aici. Această metodă există doar ca să dispară eroarea.
        }
    }
}