﻿using System;
using System.Collections.Generic;
using Addr = System.UInt32;

namespace Emul809or
{

    public class WatchEventArgs : EventArgs
    {
        public byte Data;
        public Addr Address;
    }

    public class ERAM : IMemoryIO
    {
        private byte[] data;
        const uint size = 0x1000000 - 0x010000;
        const uint baseAddress = 0x010000;
        private bool supports16bit = true;
        public List<Addr> Watch = new List<Addr> { };

        protected virtual void OnWatch(WatchEventArgs e)
        {
            WatchEvent.Invoke(this, e);
        }
        public event EventHandler<WatchEventArgs> WatchEvent;


        public uint Size
        {
            get => size;
        }
        public uint BaseAddress
        {
            get => baseAddress;
        }
        public Byte this[uint index]
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

        public ERAM()
        {
            data = new byte[size];
            for (int i = 0; i < size; i++)
            {
                data[i] = 0;
            }
        }
    }
}