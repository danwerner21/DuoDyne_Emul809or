using System;
using System.Text;

namespace Emul809or
{
    public class UARTOutChangedEventArgs : EventArgs
    {
        public bool DataChanged;
        public bool ModemChanged;
    }

    public class UART : IMemoryIO
    {
        protected virtual void OnUARTOutChanged(UARTOutChangedEventArgs e)
        {
            EventHandler<UARTOutChangedEventArgs> handler = UARTOutChanged;
            if (handler != null)
            {
                handler(this, e);
            }
        }
        public event EventHandler<UARTOutChangedEventArgs> UARTOutChanged;
        public byte ModemControl { get; set; }
        private byte outdata;
        private bool xmitReady = true;
        private StringBuilder inqueue;
        const uint size = 8;  //8 registers
        private uint baseAddress;   //set in constructor
        private bool supports16bit = false;
        public bool SuspendLogging = false;

        public enum REGISTERS : byte
        {
            UART0_DATAINOUT = 0x00,
            UART1_CHECKRX = 0x01,
            UART2_INTERRUPTS = 0x02,
            UART3_LINECONTROL = 0x03,
            UART4_MODEMCONTROL = 0x04,
            UART5_LINESTATUS = 0x05,
            UART6_MODEMSTATUS = 0x06,
            UART7_SCRATCHREG = 0x07
        }
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
                return new byte[8] { 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff };
            }

            set
            {

            }

        }


        public byte this[uint index]
        {
            get
            {
                REGISTERS register = (REGISTERS)(index - baseAddress);

                switch (register)
                {
                    case REGISTERS.UART0_DATAINOUT:
                        if (inqueue.Length > 0)
                        {
                            char c = inqueue[0];
                            inqueue.Remove(0, 1);
                            return (byte)c;
                        }
                        break;
                    case REGISTERS.UART5_LINESTATUS:
                        byte r = 0;
                        if (inqueue.Length > 0) r |= 0x01;
                        if (xmitReady) r |= 0x20;
                        return r;
                }
                return 0x00;
            }
            set
            {
                REGISTERS register = (REGISTERS)(index - baseAddress);

                switch (register)
                {
                    case REGISTERS.UART0_DATAINOUT:
                        outdata = value;
                        xmitReady = false;
                        Update(true,false);
                        break;
                    case REGISTERS.UART4_MODEMCONTROL:
                        ModemControl = value;
                        Update(false,true);
                        break;
                }
            }
        }

        public void Update(bool dataChanged,bool modemChanged)
        {
            UARTOutChangedEventArgs eventArgs = new();
            eventArgs.DataChanged = dataChanged;
            eventArgs.ModemChanged = modemChanged;
            OnUARTOutChanged(eventArgs);
        }

        public UART(uint address)
        {
            baseAddress = address;
            outdata = 0x00;
            xmitReady = true;
            inqueue = new StringBuilder();
        }

        public char fetchChar()
        {
            var o = (char)outdata;
            outdata = 0x00;
            xmitReady = true;
            return o;
        }

        public void CharIn(byte ch)
        {
            if(ch!=0)
                inqueue.Append((char)ch);
        }

        public void ResetInterrupt()
        {
        }
    }
}
