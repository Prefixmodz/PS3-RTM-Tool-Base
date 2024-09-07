using System;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using CBH_WinForm_Theme_Library_NET;
using PS3Lib;

namespace CBHThmPS3RTMBase
{
    public partial class Mainform : CrEaTiiOn_Form
    {
        public static PS3API PS3 = new PS3API();
        public static PS3ManagerAPI.PS3MAPI PS3MANAPI = new PS3ManagerAPI.PS3MAPI();
        private bool threadIsRunning = false;
        public Thread GetAPI;
        public Thread Consoleinfo;
        public Mainform()
        {
            GetAPI = new Thread(new ThreadStart(GetCurrentAPI));
            Consoleinfo = new Thread(new ThreadStart(ConsoleInfo));
            InitializeComponent();
        }

        private void GetCurrentAPI()
        {
            while (threadIsRunning)
            {
                string currentAPI = PS3.GetCurrentAPIName();
                LabelCurrentAPI.Invoke((MethodInvoker)(() => { LabelCurrentAPI.Text = PS3.GetCurrentAPIName(); }));
                Thread.Sleep(500);
            }
            GetAPI.Abort();

        }
        private void ConsoleInfo()
        {
            if (PS3.GetCurrentAPI() == SelectAPI.ControlConsole)
            {
                LabelFW.Invoke((MethodInvoker)(() => { LabelFW.Text = PS3.CCAPI.GetFirmwareVersion(); }));
                LabelLV2.Invoke((MethodInvoker)(() => { LabelLV2.Text = PS3.CCAPI.GetFirmwareType(); }));
                while (threadIsRunning)
                {
                    string CELL = PS3.CCAPI.GetTemperatureCELL();
                    string RSX = PS3.CCAPI.GetTemperatureRSX();
                    LabelCELL.Invoke((MethodInvoker)(() => { LabelCELL.Text = CELL; }));
                    LabelRSX.Invoke((MethodInvoker)(() => { LabelRSX.Text = RSX; }));
                    PS3.CCAPI.ClearTargetInfo();
                    Thread.Sleep(500);
                }
            }
            else
            {
                if (PS3.GetCurrentAPI() == SelectAPI.PS3Manager || PS3MANAPI.IsConnected)
                LabelFW.Invoke((MethodInvoker)(() => { LabelFW.Text = Convert.ToString(PS3MANAPI.PS3.GetFirmwareVersion()); }));
                LabelLV2.Invoke((MethodInvoker)(() => { LabelLV2.Text = Convert.ToString(PS3MANAPI.PS3.GetFirmwareType()); }));
                while (threadIsRunning)
                {
                    uint CELL2;
                    uint RSX2;
                    PS3MANAPI.PS3.GetTemperature(out CELL2, out RSX2);
                    LabelCELL.Invoke((MethodInvoker)(() => { LabelCELL.Text = Convert.ToString(CELL2); }));
                    LabelRSX.Invoke((MethodInvoker)(() => { LabelRSX.Text = Convert.ToString(RSX2); }));
                    Thread.Sleep(500);
                }
                Consoleinfo.Abort();
            }
        }

        private void TimerDnT_Tick(object sender, EventArgs e)
        {
            LabelTime.Text = DateTime.Now.ToLongTimeString();
            LabelDate.Text = DateTime.Now.ToLongDateString();
        }

        private void Mainform_Load(object sender, EventArgs e)
        {
            PS3.TMAPI.PS3TMAPI_NET();
            threadIsRunning = true;
            GetAPI.Start();
            TimerDnT.Start();
        }

        private void RadiobtnCCAPI_CheckedChanged(object sender, EventArgs e)
        {
            PS3.ChangeAPI(SelectAPI.ControlConsole);
        }

        private void RadiobtnPS3MAPI_CheckedChanged(object sender, EventArgs e)
        {
            PS3.ChangeAPI(SelectAPI.PS3Manager);
        }

        private void RadiobtnTMAPI_CheckedChanged(object sender, EventArgs e)
        {
            PS3.ChangeAPI(SelectAPI.TargetManager);
        }

        private void btnConnect_Click(object sender, EventArgs e)
        {
            if (PS3.GetCurrentAPI() == SelectAPI.TargetManager)
            {
                if (PS3.ConnectTarget())
                {
                    PS3.TMAPI.InitComms();
                    LabelStatus.Text = "Connected";
                    LabelStatus.ForeColor = Color.Green;
                    MessageBox.Show("Connected to Console\n\nwith Target Manager API", "Connected", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    MessageBox.Show("Failed to Connect to Console!\n\nMake sure ProDG is installed!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            else if (PS3.GetCurrentAPI() == SelectAPI.ControlConsole)
            {
                if (PS3.ConnectTarget(BoxIP.Text))
                {
                    if (!Consoleinfo.IsAlive)
                    {
                        threadIsRunning = true;
                        Consoleinfo.Start();
                    }
                    PS3.Buzzer(PS3API.BuzzerMode.Single);
                    PS3.CCAPI.Notify(CCAPI.NotifyIcon.PROGRESS, "Connected");
                    LabelStatus.Text = "Connected";
                    LabelStatus.ForeColor = Color.Green;
                    MessageBox.Show("Connected to Console\n\nwith Control Console API", "Connected", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    MessageBox.Show("Failed to Connect to Console!\n\nMake sure Control Console API is Installed On Console And PC!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            else if (PS3.GetCurrentAPI() == SelectAPI.PS3Manager)
            {
                if (PS3MANAPI.ConnectTarget(BoxIP.Text, Convert.ToInt32(7887)))
                {
                    if (PS3MANAPI.IsConnected)
                    {
                        if (!Consoleinfo.IsAlive)
                        {
                            threadIsRunning = true;
                            Consoleinfo.Start();
                        }
                        //this.Comboprocs.Items.Clear();
                        foreach (uint pidProcess in PS3MANAPI.Process.GetPidProcesses())
                        {
                            if (pidProcess != 0U)
                            {

                                //this.Comboprocs.Items.Add((object)PS3MANAPI.Process.GetName(pidProcess));
                                PS3.PS3MAPI.Notify("Connected", PS3ManagerAPI.PS3MAPI.PS3_CMD.NotifyIcon.Info, PS3ManagerAPI.PS3MAPI.PS3_CMD.NotifySound.SystemOk);
                                PS3.PS3MAPI.RingBuzzer(PS3ManagerAPI.PS3MAPI.PS3_CMD.BuzzerMode.Single);
                                LabelStatus.Text = "Connected";
                                LabelStatus.ForeColor = Color.Green;
                                MessageBox.Show("Connected to Console\n\nwith PS3 Manager API", "Connected", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            }
                        }
                        //this.Comboprocs.SelectedIndex = 0;
                    }
                    else
                    {
                        MessageBox.Show("Failed to Connect to Console!\n\nMake sure PS3MAPI is enabled in WebMan Setup!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }


            private void btnDisconnect_Click(object sender, EventArgs e)
            {
            if (PS3.GetCurrentAPI() == SelectAPI.ControlConsole)
            {
                PS3.Buzzer(PS3API.BuzzerMode.Single);
                PS3.Notify("Disconnected", CCAPI.NotifyIcon.WRONGWAY);
                PS3.DisconnectTarget();
                LabelStatus.Text = "Disconnected";
                LabelStatus.ForeColor = Color.Red;
                MessageBox.Show("Console Disconnected");
            }
            if (PS3.GetCurrentAPI() == SelectAPI.PS3Manager)
            {
                PS3.PS3MAPI.RingBuzzer(PS3ManagerAPI.PS3MAPI.PS3_CMD.BuzzerMode.Single);
                PS3.PS3MAPI.Notify("Console Disconnected", PS3ManagerAPI.PS3MAPI.PS3_CMD.NotifyIcon.Caution, PS3ManagerAPI.PS3MAPI.PS3_CMD.NotifySound.SystemOk);
                PS3.DisconnectTarget();
                LabelStatus.Text = "Disconnected";
                LabelStatus.ForeColor = Color.Red;
                MessageBox.Show("Console Disconnected");
            }
            if (PS3.GetCurrentAPI() == SelectAPI.TargetManager)
            {
                PS3.DisconnectTarget();
                LabelStatus.Text = "Disconnected";
                LabelStatus.ForeColor = Color.Red;
                MessageBox.Show("Console Disconnected");
            }
        }

        private void btnAttach_Click(object sender, EventArgs e)
        {
            if (PS3.GetCurrentAPI() == SelectAPI.ControlConsole) PS3.CCAPI.SUCCESS(PS3.CCAPI.AttachProcess());
            {
                try
                {
                    PS3.AttachProcess();
                    PS3.Notify(PS3.GetCurrentAPIName() + "   Attached to current game process!", CCAPI.NotifyIcon.PROGRESS);
                    PS3.Buzzer(PS3API.BuzzerMode.Double);
                    LabelStatus.Text = "Connected + Attached";
                    LabelStatus.ForeColor = Color.Green;
                    MessageBox.Show(PS3.GetCurrentAPI() + "  Attached to current game process!");
                }
                catch
                {
                    MessageBox.Show("Make sure you have a game running!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            if (PS3.GetCurrentAPI() == SelectAPI.PS3Manager)
            {
                try
                {
                    PS3MANAPI.Process.GetPidProcesses();
                    PS3MANAPI.AttachProcess(PS3MANAPI.Process.Processes_Pid[0]); 
                    if (PS3MANAPI.IsAttached)
                    {
                        PS3.Buzzer(PS3API.BuzzerMode.Double);
                        PS3.PS3MAPI.Notify(PS3.GetCurrentAPIName() + "   Attached to current game process!", PS3ManagerAPI.PS3MAPI.PS3_CMD.NotifyIcon.Info, PS3ManagerAPI.PS3MAPI.PS3_CMD.NotifySound.SystemOk);
                        LabelStatus.Text = "Connected + Attached";
                        LabelStatus.ForeColor = Color.Green;
                        MessageBox.Show(PS3.GetCurrentAPI() + "   Attached to current game process!");
                    }
                }
                catch
                {
                    MessageBox.Show("Make sure you have a game running!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            if (PS3.GetCurrentAPI() == SelectAPI.TargetManager)
            {
                try
                {

                    PS3.AttachProcess();
                    LabelStatus.Text = "Connected + Attached";
                    LabelStatus.ForeColor = Color.Green;
                    MessageBox.Show(PS3.GetCurrentAPI() + "   Attached to current game process!");
                }
                catch
                {
                    MessageBox.Show("Make sure you have a game running!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

            private void btnSendNoti_Click(object sender, EventArgs e)
            {
            if (PS3.GetCurrentAPI() == SelectAPI.PS3Manager)
            {
                PS3.PS3MAPI.Notify(BoxNotify.Text, PS3ManagerAPI.PS3MAPI.PS3_CMD.NotifyIcon.Info, PS3ManagerAPI.PS3MAPI.PS3_CMD.NotifySound.SystemOk);
            }
            if (PS3.GetCurrentAPI() == SelectAPI.ControlConsole)
            {
                PS3.CCAPI.Notify(CCAPI.NotifyIcon.INFO, BoxNotify.Text);
            }
        }

        private void btnSoftReboot_Click(object sender, EventArgs e)
        {
            PS3.Power(PS3API.PowerFlags.SoftReboot);
        }

        private void btnHardReboot_Click(object sender, EventArgs e)
        {
            PS3.Power(PS3API.PowerFlags.HardReboot);
        }

        private void btnShutdown_Click(object sender, EventArgs e)
        {
            PS3.Power(PS3API.PowerFlags.ShutDown);
        }

        private void btnQuickReboot_Click(object sender, EventArgs e)
        {
            PS3.Power(PS3API.PowerFlags.QuickReboot);
        }

        private void btnsetIDPS_Click(object sender, EventArgs e)
        {
            if (BoxIDPS.Text.Length != 0x20)
            {
                MessageBox.Show("IDPS Must Be 32 characters long!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            else
            {
                PS3.SetConsoleID(BoxIDPS.Text);
                MessageBox.Show(BoxIDPS.Text, "IDPS Set", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
            }
        }

        private void btnSetPSID_Click(object sender, EventArgs e)
        {
            if (BoxPSID.Text.Length != 0x20)
            {
                MessageBox.Show("PSID Must Be 32 characters long!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            else
            {
                PS3.SetPSID(BoxPSID.Text);
                MessageBox.Show(BoxPSID.Text, "PSID Set", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
            }
        }

        private void btnSetBootIDPS_Click(object sender, EventArgs e)
        {
            if (PS3.GetCurrentAPI() == SelectAPI.PS3Manager)
            {
                if (BoxIDPS.Text.Length != 0x20)
                {
                    MessageBox.Show("IDPS Must Be 32 characters long!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                else
                {
                    MessageBox.Show("Can not set a Boot IDPS With PS3 Manager API", "!", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                if (PS3.GetCurrentAPI() == SelectAPI.ControlConsole)
                {
                    if (BoxIDPS.Text.Length != 0x20)
                    {
                        MessageBox.Show("IDPS Must Be 32 characters long!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    else
                    {
                        PS3.CCAPI.SetBootConsoleID(BoxIDPS.Text, CCAPI.IdType.IDPS);
                        MessageBox.Show(BoxIDPS.Text, "Boot IDPS Set", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
                    }
                }
            }
        }

        private void btnResetBootIDPS_Click(object sender, EventArgs e)
        {
            if (PS3.GetCurrentAPI() == SelectAPI.PS3Manager)
            {
                MessageBox.Show("There is no Boot IDPS to Clear Because you can not set a Boot IDPS With PS3 Manager API", "!", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            if (PS3.GetCurrentAPI() == SelectAPI.ControlConsole)
            {
                PS3.CCAPI.ResetBootConsoleID(CCAPI.IdType.IDPS);
                MessageBox.Show("Boot IDPS Reset", "Boot IDPS Cleared", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
            }
        }

        private void btnSetbootPSID_Click(object sender, EventArgs e)
        {
            if (PS3.GetCurrentAPI() == SelectAPI.PS3Manager)
            {
                if (BoxPSID.Text.Length != 0x20)
                {
                    MessageBox.Show("PSID Must Be 32 characters long!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                else
                {
                    MessageBox.Show("Can not set a Boot PSID With PS3 Manager API", "!", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                if (PS3.GetCurrentAPI() == SelectAPI.ControlConsole)
                {
                    if (BoxPSID.Text.Length != 0x20)
                    {
                        MessageBox.Show("PSID Must Be 32 characters long!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    else
                    {
                        PS3.CCAPI.SetBootConsoleID(BoxPSID.Text, CCAPI.IdType.PSID);
                        MessageBox.Show(BoxPSID.Text, "Boot PSID Set", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
                    }
                }
            }
        }

        private void btnResetBootPSID_Click(object sender, EventArgs e)
        {
            if (PS3.GetCurrentAPI() == SelectAPI.PS3Manager)
            {
                MessageBox.Show("There is no Boot PSID to Clear Because you can not set a Boot PSID With PS3 Manager API", "!", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            if (PS3.GetCurrentAPI() == SelectAPI.ControlConsole)
            {
                PS3.CCAPI.ResetBootConsoleID(CCAPI.IdType.PSID);
                MessageBox.Show("Boot PSID Reset", "Boot PSID Cleared", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
            }
        }

        private uint[] procs;
        private void crEaTiiOn_Ultimate_FancyButton2_Click(object sender, EventArgs e)
        {
            if (PS3.GetCurrentAPI() == SelectAPI.ControlConsole)
            {
                procs = new uint[64];
                PS3.CCAPI.GetProcessList(out procs);
                Comboprocs.Items.Clear();
                for (int i = 0; i < procs.Length; i++)
                {
                    string sandwich = String.Empty;
                    PS3.CCAPI.GetProcessName(procs[i], out sandwich);
                    Comboprocs.Items.Add(sandwich);
                }
                procs = null;
            }
            if (PS3.GetCurrentAPI() == SelectAPI.PS3Manager)
            {
                this.Comboprocs.Items.Clear();
                foreach (uint sosajbutty in PS3MANAPI.Process.GetPidProcesses())
                {
                    if (sosajbutty == 0U)
                    {
                        break;
                    }
                    this.Comboprocs.Items.Add(PS3MANAPI.Process.GetName(sosajbutty));
                }
                this.Comboprocs.SelectedIndex = 0;
            }
        }

        private void crEaTiiOn_Ultimate_FancyButton3_Click(object sender, EventArgs e)
        {
            if (PS3.GetCurrentAPI() == SelectAPI.PS3Manager)
            {
                if (Comboprocs.SelectedIndex == 0)
                {
                    uint SelectedProc = procs[Comboprocs.SelectedIndex = 0];
                    PS3MANAPI.AttachProcess(SelectedProc);
                    PS3.PS3MAPI.RingBuzzer(PS3ManagerAPI.PS3MAPI.PS3_CMD.BuzzerMode.Single);
                    PS3.PS3MAPI.Notify(Comboprocs.Text, PS3ManagerAPI.PS3MAPI.PS3_CMD.NotifyIcon.Info, PS3ManagerAPI.PS3MAPI.PS3_CMD.NotifySound.SystemOk);
                    MessageBox.Show("Success!");
                }
                else
                {
                    MessageBox.Show("Make sure you have selected a Process", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            else if (PS3.GetCurrentAPI() == SelectAPI.ControlConsole)
                {
                    if (Comboprocs.SelectedIndex == 0)
                    {
                        try
                        {

                            uint SelectedProc = procs[Comboprocs.SelectedIndex];
                            PS3.CCAPI.AttachProcess(SelectedProc);
                            PS3.CCAPI.RingBuzzer(CCAPI.BuzzerMode.Single);
                            PS3.CCAPI.Notify(CCAPI.NotifyIcon.INFO, Comboprocs.Text); //"CCAPI Attached To:\n\n" + Comboprocs.Text
                            MessageBox.Show("Success!");
                        }
                        catch
                        {
                            MessageBox.Show("Make sure you have selected a Process", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                }
            }

        private void CheckboxWAWGDMode_CheckedChanged(object sender)
        {
            if (CheckboxWAWGDMode.Checked)
            {
                byte[] gdmode;
                gdmode = new byte[] { 0x08 };
                PS3.SetMemory(0x011c0bdf, gdmode);
            }
            else
            {
                byte[] gdmodeoff;
                gdmodeoff = new byte[] { 0x02 };
                PS3.SetMemory(0x011c0bdf, gdmodeoff);
            }
        }

        private void CheckboxWAWNoclip_CheckedChanged(object sender)
        {
            if (CheckboxWAWNoclip.Checked)
            {
                byte[] NoShit;
                NoShit = new byte[] { 0x01 };
                PS3.SetMemory(0x011c0c7b, NoShit);
            }
            else
            {
                byte[] NoShitoff;
                NoShitoff = new byte[] { 0x00 };
                PS3.SetMemory(0x011c0c7b, NoShitoff);
            }
        }

        private void btnWAWSetname_Click(object sender, EventArgs e)
        {
            PS3.Extension.WriteString(0x02952934, BoxWAWName.Text);
        }

        private void btnWAWGetname_Click(object sender, EventArgs e)
        {
            string Neongay;
            Neongay = PS3.Extension.ReadString(0x02952934);
            BoxWAWName.Text = Neongay;
        }
    }
}
