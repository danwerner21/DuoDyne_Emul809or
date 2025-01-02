using System;

namespace Emul809or
{
    public class FrontPanelOutChangedEventArgs : EventArgs
    {
        public bool DataChanged;
        public bool Reset;
    }

    public class SBC : IMemoryIO
    {
        protected virtual void OnFrontPanelOutChanged(FrontPanelOutChangedEventArgs e)
        {
            EventHandler<FrontPanelOutChangedEventArgs> handler = FrontPanelOutChanged;
            if (handler != null)
            {
                handler(this, e);
            }
        }
        public event EventHandler<FrontPanelOutChangedEventArgs> FrontPanelOutChanged;

        private byte leds;
        private byte switches;
        private byte bank0 = 0;
        private byte bank1 = 0;
        private byte bank2 = 0;
        private byte bank3 = 0;

        public bool PageEnable { get; set; } = false;

        const uint size = 1;  //4 registers
        private uint baseAddress;   //set in constructor
        private bool supports16bit = false;
        public bool SuspendLogging = false;


        public uint Size
        {
            get => size;
        }
        public uint BaseAddress
        {
            get => baseAddress;
        }
        public bool Supports16Bit
        {
            get => supports16bit;
        }


        public byte[] MemoryBytes
        {
            get
            {
                var result= new byte[6] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };
                result[0] = bank0;
                result[1] = bank1;
                result[2] = bank2;
                result[3] = bank3;
                result[4] = switches;
                return result;
            }
            set
            {
                bank0 = value[0];
                bank1 = value[1];
                bank2 = value[2];
                bank3 = value[3];
                leds = value[4];
            }
        }


        public byte this[uint index]
        {
            get
            {
                switch (index - baseAddress)
                {
                    case 0:
                        return bank0;
                    case 1:
                        return bank1;
                    case 2:
                        return bank2;
                    case 3:
                        return bank3;
                    case 4:
                        return switches;
                    case 5:
                        Reset();
                        return 0;
                    default:
                        return 0;
                }
            }
            set
            {
                switch (index - baseAddress)
                {
                    case 0:
                        bank0 = value;
                        break;
                    case 1:
                        bank1 = value;
                        break;
                    case 2:
                        bank2 = value;
                        break;
                    case 3:
                        bank3 = value;
                        break;
                    case 4:
                        leds = value;
                        break;
                    case 5:
                        Reset();
                        break;
                    default:
                        break;
                }
                Update(true);
            }
        }

        public void Update(bool dataChanged)
        {
            FrontPanelOutChangedEventArgs eventArgs = new();
            eventArgs.DataChanged = dataChanged;
            eventArgs.Reset = false;
            OnFrontPanelOutChanged(eventArgs);
        }

        public void Reset()
        {
            FrontPanelOutChangedEventArgs eventArgs = new();
            eventArgs.DataChanged = false;
            eventArgs.Reset = true;
            OnFrontPanelOutChanged(eventArgs);
        }


        public byte LEDS
        {
            get
            {
                return leds;
            }
        }

        public byte SWITCHES
        {
            set
            {
                switches = value;
            }
        }

        public byte BANK0
        {
            get
            {
                return bank0;
            }
        }

        public byte BANK1
        {
            get
            {
                return bank1;
            }
        }

        public byte BANK2
        {
            get
            {
                return bank2;
            }
        }

        public byte BANK3
        {
            get
            {
                return bank3;
            }
        }


        public SBC(uint address)
        {
            baseAddress = address;
            leds = 0;
            switches = 0;
        }

    }
}


