using System;
using System.Text;
using System.Windows.Forms;

namespace Emul809or
{
    public partial class MemoryViewer : Form
    {
        ROM rom;
        RAM ram;
        ERAM eram;

        public MemoryViewer(ROM _rom, RAM _ram, ERAM _eram)
        {
            rom = _rom;
            ram = _ram;
            eram = _eram;
            InitializeComponent();
        }

        private void memoryDeviceCombo_SelectedIndexChanged(object sender, EventArgs e)
        {
            switch (memoryDeviceCombo.SelectedIndex)
            {
                case 1:             //ROM
                    noteLabel.Text = "*First 32K addresses used by RAM\n Start at 008000";
                    FillRTB(rom);
                    JumpToAddress((rom.BaseAddress + 0x8000).ToString("X6"));
                    break;
                case 2:             //RAM
                    noteLabel.Text = "";
                    FillRTB(ram);
                    JumpToAddress(ram.BaseAddress.ToString("X6"));
                    break;
                case 3:             //ERAM
                    noteLabel.Text = "";
                    FillRTB(eram);
                    JumpToAddress(eram.BaseAddress.ToString("X6"));
                    break;
                default:
                    noteLabel.Text = "";
                    rtb.Clear();
                    break;
            }

            void FillRTB(IMemoryIO device)
            {
                uint addr;
                StringBuilder sb = new StringBuilder();
                uint x = 0;
                while (x < device.Size)
                {
                    addr = device.BaseAddress + x;
                    sb.Append(addr.ToString("X6") + ": ");
                    for (int y = 0; y < 16; y++)
                    {
                        if ((y + x) < device.Size)
                            sb.Append(device.MemoryBytes[y + x].ToString("X2") + " ");
                    }
                    sb.Append(" ");
                    for (int y = 0; y < 16; y++)
                    {
                        if ((x) < device.Size)
                            if ((device.MemoryBytes[x] > 31) && (device.MemoryBytes[x] < 127))
                            {
                                sb.Append((char)device.MemoryBytes[x]);
                            }
                            else
                            {
                                sb.Append('.');
                            }
                        x++;
                    }
                    sb.AppendLine(" ");
                }
                rtb.Text = sb.ToString();
            }
        }

        void JumpToAddress(string addr)
        {
            int loc = rtb.Find(addr);
            if (loc > -1)
            {
                rtb.SelectionStart = loc;
                rtb.ScrollToCaret();
            }
            else
            {
                MessageBox.Show("No match");
            }
        }
        private void MemoryViewer_Load(object sender, EventArgs e)
        {
            noteLabel.Text = "";
        }

        private void jumpButton_Click(object sender, EventArgs e)
        {
            JumpToAddress(jumpTextBox.Text);
        }
    }
}
