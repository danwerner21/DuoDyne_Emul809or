using System;

namespace Emul809or
{
    interface IMemoryIO
    {
        uint Size
        {
            get;
        }
        uint BaseAddress
        {
            get;
        }
        bool Supports16Bit
        {
            get;
        }
        Byte this[uint index]
        {
            get;
            set;
        }
        byte[] MemoryBytes
        {
            get;
        }
    }
}
