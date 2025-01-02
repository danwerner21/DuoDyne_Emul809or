using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
using Addr = System.UInt32;

namespace Emul809or
{
    public class RAM : IMemoryIO
    {
        private byte[] data;
        const uint size = 32768;
        const uint baseAddress = 0x000000;

        public List<Addr> Watch = new List<Addr> { };

        protected virtual void OnWatch(WatchEventArgs e)
        {
            WatchEvent.Invoke(this, e);
        }
        public event EventHandler<WatchEventArgs> WatchEvent;



        private bool supports16bit = true;
        public uint Size
        {
            get => size;
        }
        public uint BaseAddress
        {
            get => baseAddress;
        }
        public byte this[uint index]
        {
            get => data[index - baseAddress];
            set
            {
                data[index - baseAddress] = value;
                if (Watch.Contains(index))
                {
                    try
                    {
                        WatchEventArgs e = new WatchEventArgs();
                        e.Data = value;
                        e.Address = index;
                        WatchEvent(this, e);
                    }
                    catch
                    { }
                }
            }
        }
        public bool Supports16Bit
        {
            get => supports16bit;
        }

        public byte[] MemoryBytes => data;

        public RAM()
        {
            data = new byte[size];
            for (int i = 0; i < size; i++)
            {
                data[i] = 0;
            }
        }

        public RAM(string ramFile)
        {
            data = new byte[size];
            for (int i = 0; i < size; i++)
            {
                data[i] = 0;
            }
            if (ramFile.Length > 0)
            {
                try
                {
                    FileStream fs = File.OpenRead(ramFile);
                    for (int i = 0; i < size; i++)
                    {
                        data[i] = (byte)fs.ReadByte();
                    }
                }
                catch (Exception)
                {
                    MessageBox.Show("Unable to load specified RAM file. Continuing...");
                }
            }
        }

    }
}
