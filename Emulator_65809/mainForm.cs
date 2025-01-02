using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace Emul809or
{
    public partial class mainForm : Form
    {
        CPU cpu;

        string ROMlocation;
        string RAMlocation;
        string Image1location;
        string Image2location;

        public bool SuspendLogging;
        public ushort speed;
        bool breakActive;
        long cyclesPrev = 0;
        const int MAXLENGTH = 500;

        string LogFileName = "";
        bool LogToFile = false;
        StreamWriter logger;

        ROM rom;
        RAM ram;
        ERAM eram;
        UART uart1;   // Console Port
        PPIDE ppide;  // PPIDE Interface
        SBC frontPanel;
        NullDevice nullDev;

        public mainForm()
        {
            InitializeComponent();
        }

        private void mainForm_Load(object sender, EventArgs e)
        {
            ReloadForm();
        }

        private void ReloadForm()
        {
            try
            {
                //read last ROM location
                RegistryKey keyROM = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Emul809or");
                if (keyROM != null)
                {
                    ROMlocation = keyROM.GetValue("MRU_ROM").ToString();
                    try
                    {
                        RAMlocation = keyROM.GetValue("MRU_RAM").ToString();
                    }
                    catch { }
                    Image1location = (keyROM.GetValue("MRU_IMAGE1") ?? "").ToString();
                    Image2location = (keyROM.GetValue("MRU_IMAGE2") ?? "").ToString();
                    if (Image1location == Image2location) Image2location = "";
                }
                else
                {
                    MessageBox.Show("No ROM information. Please select ROM for initialization.", "ROM required");
                    GetROM();
                }
                labImages.Text = ("Drive1:" + (Image1location ?? "") + "\n" + "Drive1:" + (Image2location ?? "") + "\n");
                LoadObjects();
                speed = 800;
                ClearSpeedChecks();
                loggingToolStripMenuItem1.Checked = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }

        }

        private void GetROM()
        {
            try
            {
                openFileDialog1.Filter = "bin files (*.bin)|*.bin";
                openFileDialog1.FileName = "";
                openFileDialog1.Title = "ROM Image";
                if (openFileDialog1.ShowDialog() == DialogResult.OK)
                {
                    ROMlocation = openFileDialog1.FileName;
                    currentROMLabel.Text = ROMlocation;
                    //store for next open
                    RegistryKey keyROM = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\Emul809or");
                    keyROM.SetValue("MRU_ROM", openFileDialog1.FileName);
                    keyROM.Close();
                }
                else
                {
                    //do something...
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void GetRAM()
        {
            try
            {
                openFileDialog1.Filter = "bin files (*.bin)|*.bin";
                openFileDialog1.FileName = "";
                openFileDialog1.Title = "RAM Image";
                if (openFileDialog1.ShowDialog() == DialogResult.OK)
                {
                    ROMlocation = openFileDialog1.FileName;
                    currentROMLabel.Text = ROMlocation;
                    //store for next open
                    RegistryKey keyRAM = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\Emul809or");
                    keyRAM.SetValue("MRU_RAM", openFileDialog1.FileName);
                    keyRAM.Close();
                }
                else
                {
                    //do something...
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void GetImage1()
        {
            try
            {
                openFileDialog1.Filter = "img files (*.img)|*.img";
                openFileDialog1.FileName = "";
                openFileDialog1.Title = "IDE Primary Image";
                if (openFileDialog1.ShowDialog() == DialogResult.OK)
                {
                    Image1location = openFileDialog1.FileName;
                    //store for next open
                    RegistryKey keyROM = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\Emul809or");
                    keyROM.SetValue("MRU_IMAGE1", openFileDialog1.FileName);
                    keyROM.Close();
                }
                else
                {
                    //do something...
                }
                labImages.Text = ("Drive1:" + (Image1location ?? "") + "\n" + "Drive1:" + (Image2location ?? "") + "\n");
                ppide.reOpen(Image1location, Image2location);  // ppide interface
                WriteLog("PPIDE added\t\t\t0x000388-0x00038B\n");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
        private void GetImage2()
        {
            try
            {
                openFileDialog1.Filter = "img files (*.img)|*.img";
                openFileDialog1.Title = "IDE Secondary Image";
                openFileDialog1.FileName = "";
                if (openFileDialog1.ShowDialog() == DialogResult.OK)
                {
                    Image2location = openFileDialog1.FileName;
                    //store for next open
                    RegistryKey keyROM = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\Emul809or");
                    keyROM.SetValue("MRU_IMAGE2", openFileDialog1.FileName);
                    keyROM.Close();
                }
                else
                {
                    //do something...
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            labImages.Text = ("Drive1:" + (Image1location ?? "") + "\n" + "Drive2:" + (Image2location ?? "") + "\n");
            ppide.reOpen(Image1location, Image2location);  // ppide interface
            WriteLog("PPIDE added\t\t\t0x000388-0x00038B\n");
        }


        private void loggingToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            if (sender == null)
            {
                //only do this if manually calling this event (not from actual click)
                loggingToolStripMenuItem1.Checked = !loggingToolStripMenuItem1.Checked;
            }

            ClearSpeedChecks();
            if (!loggingToolStripMenuItem1.Checked)
            {
                fastestToolStripMenuItem1.Checked = true;
                speed = 1000;
            }
            else
            {
                eight00ToolStripMenuItem.Checked = true;
                speed = 800;
            }
            cpu.SuspendLogging = !loggingToolStripMenuItem1.Checked;
            this.SuspendLogging = cpu.SuspendLogging;
        }

        private void stopToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                cyclesTimer.Enabled = false;
                cpu.Stop();
                SuspendLogging = true;
                WriteLog("\n*************** STOP *****************\n");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }

        }

        private void resetToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                //TO DO Reset all state data from all objects of concern
                SuspendLogging = false;
                loggingToolStripMenuItem1.Checked = true;
                WriteLog("\n*************** RESET ****************\n");
                WriteLog("*** Loading Debug Info... ");
                WriteLog("Complete ***\n");
                cpu.Reset();
                cyclesTimer.Enabled = true;
                Run();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        void LoadObjects()
        {
            try
            {
                currentROMLabel.Text = ROMlocation;
                WriteLog("Initializing...\n");
                ram = new RAM(RAMlocation);
                ram.WatchEvent += Ram_WatchEvent;
                WriteLog("RAM added\t\t\t0x000000-0x007FFF\n");
                rom = new ROM(ROMlocation);
                WriteLog("ROM added\t\t\t0x008000-0x00FFFF\n");
                eram = new ERAM();
                eram.WatchEvent += Ram_WatchEvent;
                WriteLog("ERAM added\t\t\t0x080000-0x1FFFFF\n");
                uart1 = new UART(0xDF58);  //Console Port
                uart1.UARTOutChanged += Uart1_UARTOutChanged;
                WriteLog("Console added\t\t\t0x00DF58-0x00DF5F\n");
                ppide = new PPIDE(0xDF88, Image1location, Image2location);  // ppide interface
                WriteLog("PPIDE added\t\t\t0x00DF88-0x00DF8B\n");
                Terminal terminal = new Terminal(uart1);
                WriteLog("Console Terminal Telnet Server\n");
                frontPanel = new SBC(0xDF50);
                WriteLog("Front Panel added\t\t\t0x00DF50-0x00DF55\nn");
                frontPanel.FrontPanelOutChanged += FrontPanel_FrontPanelOutChanged;
                nullDev = new NullDevice();
                WriteLog("NullDev added\t\t\t0x******\n");
                cpu = new CPU(rom, ram, eram, uart1, ppide, frontPanel, nullDev);
                cpu.StatusChanged += cpu_StatusChanged;
                cpu.LogTextUpdate += cpu_LogTextUpdate;
                cpu.Break += Cpu_Break;
                WriteLog("*** G1000 TO BOOT CUBIX\n");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void Uart1_UARTOutChanged(object sender, UARTOutChangedEventArgs e)
        {
            if (e.ModemChanged)
            {
                frontPanel.PageEnable = false;
                if ((uart1.ModemControl & 0x04) == 0) frontPanel.PageEnable = true;
            }
        }

        private void Ram_WatchEvent(object sender, WatchEventArgs e)
        {
            for (int i = 0; i < lstWatch.Items.Count; i++)
            {
                if (Convert.ToUInt32(lstWatch.Items[i].ToString().Split(':')[0], 16) == e.Address)
                {
                    lstWatch.Items[i] = e.Address.ToString("X6") + ":" + e.Data.ToString("X2") + "(" + e.Data.ToString() + ")";
                }
            }
        }

        private void FrontPanel_FrontPanelOutChanged(object sender, FrontPanelOutChangedEventArgs e)
        {
            FrontPanelL_0.BackColor = Color.Gray;
            FrontPanelL_1.BackColor = Color.Gray;
            FrontPanelL_2.BackColor = Color.Gray;
            FrontPanelL_3.BackColor = Color.Gray;
            FrontPanelL_4.BackColor = Color.Gray;
            FrontPanelL_5.BackColor = Color.Gray;
            FrontPanelL_6.BackColor = Color.Gray;
            FrontPanelL_7.BackColor = Color.Gray;

            if ((frontPanel.LEDS & 0x01) > 0) FrontPanelL_0.BackColor = Color.Red;
            if ((frontPanel.LEDS & 0x02) > 0) FrontPanelL_1.BackColor = Color.Red;
            if ((frontPanel.LEDS & 0x04) > 0) FrontPanelL_2.BackColor = Color.Red;
            if ((frontPanel.LEDS & 0x08) > 0) FrontPanelL_3.BackColor = Color.Red;
            if ((frontPanel.LEDS & 0x10) > 0) FrontPanelL_4.BackColor = Color.Red;
            if ((frontPanel.LEDS & 0x20) > 0) FrontPanelL_5.BackColor = Color.Red;
            if ((frontPanel.LEDS & 0x40) > 0) FrontPanelL_6.BackColor = Color.Red;
            if ((frontPanel.LEDS & 0x80) > 0) FrontPanelL_7.BackColor = Color.Red;

            labBank0.Text = "BANK0-" + frontPanel.BANK0.ToString("X2");
            labBank1.Text = "BANK1-" + frontPanel.BANK1.ToString("X2");
            labBank2.Text = "BANK2-" + frontPanel.BANK2.ToString("X2");
            labBank3.Text = "BANK3-" + frontPanel.BANK3.ToString("X2");

            if (e.Reset) cpu.Reset();
        }

        private void Cpu_Break(object sender, BreakEventArgs e)
        {

            loggingToolStripMenuItem1.Checked = true;
            eight00ToolStripMenuItem.Checked = true;
            cpu.SuspendLogging = false;
            this.SuspendLogging = false;
            breakActive = true;
            breakToolStripMenuItem.Text = "Res&ume";
            stepToolStripMenuItem.Enabled = true;
            labBreak.Text = "** BREAK **";
        }


        void Run()
        {
            try
            {
                while (!cpu.IsStopped() && !breakActive)
                {
                    labBreak.Text = "";
                    cpu.Step();
                    Application.DoEvents();
                    System.Threading.Thread.Sleep(1000 - speed);
                }
                if (cpu.IsStopped() && !this.IsDisposed)
                {
                    WriteLog("\n************** STP *****************\n");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
        void WriteLog(string newText)
        {
            if (LogToFile)
            {
                logger.WriteLine(newText);
            }
            else
            {
                logText.SuspendLayout();
                if (logText.Lines.Length > MAXLENGTH)
                {
                    List<string> lines = logText.Lines.ToList();
                    lines.RemoveRange(0, lines.Count - MAXLENGTH);        //only keep 100 lines
                    logText.Lines = lines.ToArray();
                }
                logText.AppendText(newText);
                logText.SelectionStart = logText.Text.Length;
                logText.ScrollToCaret();
                logText.ResumeLayout();
            }
        }

        private void cpu_LogTextUpdate(object sender, LogTextUpdateEventArgs e)
        {
            WriteLog(e.NewText + "\n");
        }

        private void cpu_StatusChanged(object sender, StatusChangedEventArgs e)
        {
            if (SuspendLogging) { return; }

            statusGroup.SuspendLayout();
            statusA.Text = e.A.ToString("X2");
            statusB.Text = e.B.ToString("X2");
            statusX.Text = e.X.ToString("X4");
            statusY.Text = e.Y.ToString("X4");
            statusS.Text = e.S.ToString("X4");
            statusU.Text = e.U.ToString("X4");
            statusDP.Text = e.DP.ToString("X2");
            flagsE.Text = BoolToString(e.FlagE);
            flagsF.Text = BoolToString(e.FlagF);
            flagsH.Text = BoolToString(e.FlagH);
            flagsI.Text = BoolToString(e.FlagI);
            flagsN.Text = BoolToString(e.FlagN);
            flagsZ.Text = BoolToString(e.FlagZ);
            flagsV.Text = BoolToString(e.FlagV);
            flagsC.Text = BoolToString(e.FlagC);
            statusPC.Text = e.PC.ToString("X4");
            statusCycles.Text = e.Cycles.ToString("X8");
            //statusGroup.Refresh();
            statusGroup.ResumeLayout();
        }

        static string BoolToString(bool boolVal)
        {
            if (boolVal)
            {
                return "1";
            }
            else
            {
                return "0";
            }
        }
        void ClearSpeedChecks()
        {
            slowestToolStripMenuItem1.Checked = false;
            fastestToolStripMenuItem1.Checked = false;
            eight00ToolStripMenuItem.Checked = false;
        }
        private void slowestToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            ClearSpeedChecks();
            speed = 0;
        }

        private void fastestToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            ClearSpeedChecks();
            speed = 1000;
        }

        private void manualSpeedToolStripTextBox2_Click(object sender, EventArgs e)
        {
            ClearSpeedChecks();
            speed = UInt16.Parse(manualSpeedToolStripTextBox2.Text);
        }

        private void eight00ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ClearSpeedChecks();
            speed = 800;
        }




        private void breakToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (!breakActive)
            {
                breakActive = true;
                breakToolStripMenuItem.Text = "Res&ume";
                stepToolStripMenuItem.Enabled = true;
            }
            else
            {
                breakActive = false;
                cpu.Go();
                breakToolStripMenuItem.Text = "&Break";
                stepToolStripMenuItem.Enabled = false;
                Run();
            }
        }


        private void stepToolStripMenuItem_Click(object sender, EventArgs e)
        {
            cpu.Step();
        }

        private void memoryViewerToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MemoryViewer mv = new(rom, ram, eram);
            mv.Show();
        }


        private void cyclesTimer_Tick(object sender, EventArgs e)
        {
            if (!cpu.IsStopped())
            {
                long cyclesCurrent = cpu.TotalCycles;
                long clockEquiv = cyclesCurrent - cyclesPrev;
                if (clockEquiv < 1000)
                {
                    clockEquivLabel.Text = clockEquiv.ToString("D") + " Hz";
                }
                else if (clockEquiv < 1000000)
                {
                    clockEquivLabel.Text = (clockEquiv / (decimal)1000).ToString("F2") + " kHz";
                }
                else
                {
                    clockEquivLabel.Text = (clockEquiv / (decimal)1000000).ToString("F2") + " MHz";
                }
                cyclesPrev = cyclesCurrent;

            }
        }

        private void mainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            ppide.Close();
            if (!cpu.IsStopped())
            {
                cpu.Stop();
            }
        }


        private void selectRomImageToolStripMenuItem_Click(object sender, EventArgs e)
        {
            GetROM();
            rom = new ROM(ROMlocation);
            WriteLog("ROM added\t\t\t0x008000-0x00FFFF\n");
        }

        private void selectDisk1ImageToolStripMenuItem_Click(object sender, EventArgs e)
        {
            GetImage1();
        }

        private void selectDisk2ImageToolStripMenuItem_Click(object sender, EventArgs e)
        {
            GetImage2();
        }

        private void mnuAdd_Click(object sender, EventArgs e)
        {
            GetBreakpoint getBreakpoint = new GetBreakpoint();
            getBreakpoint.ShowDialog();
            if (getBreakpoint.Address > 0)
            {
                cpu.BreakpointAddress.Add(getBreakpoint.Address);
                RefreshBreakpoints();
            }
        }

        private void RefreshBreakpoints()
        {
            lstBreakpoints.Items.Clear();
            foreach (var n in cpu.BreakpointAddress)
            {
                lstBreakpoints.Items.Add("0x" + n.ToString("X6"));
            }
        }

        private void mnuDelete_Click(object sender, EventArgs e)
        {
            var x = lstBreakpoints.SelectedIndex;
            try
            {
                cpu.BreakpointAddress.Remove(cpu.BreakpointAddress[x]);
            }
            catch { }
            RefreshBreakpoints();
        }

        private void FrontPanels_CheckedChanged(object sender, EventArgs e)
        {
            byte value = 0;
            if (FrontPanels_0.Checked) value |= 0x01;
            if (FrontPanels_1.Checked) value |= 0x02;
            if (FrontPanels_2.Checked) value |= 0x04;
            if (FrontPanels_3.Checked) value |= 0x08;
            if (FrontPanels_4.Checked) value |= 0x10;
            if (FrontPanels_5.Checked) value |= 0x20;
            if (FrontPanels_6.Checked) value |= 0x40;
            if (FrontPanels_7.Checked) value |= 0x80;
            frontPanel.SWITCHES = value;
        }

        private void centerMouseForCaptureToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                openFileDialog1.Filter = "log files (*.log)|*.log";
                openFileDialog1.FileName = "";
                openFileDialog1.Title = "LOG FILE";
                if (openFileDialog1.ShowDialog() == DialogResult.OK)
                {
                    LogFileName = openFileDialog1.FileName;
                    LogToFile = true;
                    logger = new StreamWriter(LogFileName);
                }
                else
                {
                    //do something...
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void stopFileLoggingToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                logger.Close();
                LogToFile = false;
            }
            catch { }
        }

        private void mnuAdd1_Click(object sender, EventArgs e)
        {
            GetBreakpoint getBreakpoint = new GetBreakpoint();
            getBreakpoint.ShowDialog();
            if (getBreakpoint.Address > 0xFFFF)
            {
                eram.Watch.Add(getBreakpoint.Address);

            }
            else
            {
                ram.Watch.Add(getBreakpoint.Address);
            }
            RefreshWatches();
        }

        private void RefreshWatches()
        {
            lstWatch.Items.Clear();
            foreach (var n in ram.Watch)
            {
                lstWatch.Items.Add("0x" + n.ToString("X6"));
            }
            foreach (var n in eram.Watch)
            {
                lstWatch.Items.Add("0x" + n.ToString("X6"));
            }

        }

        private void mnuDelete1_Click(object sender, EventArgs e)
        {
            UInt32 x = 0;

            try
            {
                x = Convert.ToUInt32(lstWatch.SelectedItem.ToString(), 16);
            }
            catch
            { }

            if (x > 0xFFFF)
            {
                try
                {
                    eram.Watch.Remove(x);
                }
                catch { }
            }
            else
            {
                try
                {
                    ram.Watch.Remove(x);
                }
                catch { }
            }

            RefreshWatches();
        }

        private void selectRamImageToolStripMenuItem_Click(object sender, EventArgs e)
        {
            GetRAM();
            ram = new RAM(RAMlocation);
            WriteLog("RAM added\t\t\t0x000000-0x007FFF\n");
        }
    }
}