using System;
using System.IO;
using System.Windows;

namespace Emul809or
{
    public class PPIDEOutChangedEventArgs : EventArgs
    {
        public bool DataChanged;
    }

    public class PPIDE : IMemoryIO
    {
        protected virtual void OnPPIDEOutChanged(PPIDEOutChangedEventArgs e)
        {
            EventHandler<PPIDEOutChangedEventArgs> handler = PPIDEOutChanged;
            if (handler != null)
            {
                handler(this, e);
            }
        }
        public event EventHandler<PPIDEOutChangedEventArgs> PPIDEOutChanged;

        FileStream ideImage1;
        FileStream ideImage2;


        // 8255 ports/registers
        private byte idelo;
        private byte idehi;
        private byte idecntrl;
        private byte ideppic; // $92=Read Mode, $80=write mode

#pragma warning disable CS0414 
        // ide registers
        private byte PPIDE_DATA;    // idecntrl = $08 (+$40 = read, +$20 = write)
        private byte PPIDE_ERR;     // idecntrl = $09 (+$40 = read, +$20 = write)
        private byte PPIDE_SEC_CNT; // idecntrl = $0A (+$40 = read, +$20 = write)
        private byte PPIDE_LBALOW;  // idecntrl = $0B (+$40 = read, +$20 = write)
        private byte PPIDE_LBAMID;  // idecntrl = $0C (+$40 = read, +$20 = write)
        private byte PPIDE_LBAHI;   // idecntrl = $0D (+$40 = read, +$20 = write)
        private byte PPIDE_DEVICE;  // idecntrl = $0E (+$40 = read, +$20 = write)
        private byte PPIDE_COMMAND; // idecntrl = $0F (write) (+$20 = write)
        private byte PPIDE_STATUS;  // idecntrl = $0F (read)  (+$40 = read)
        private byte PPIDE_CONTROL; // idecntrl = $16 (+$40 = read, +$20 = write)
        private byte PPIDE_ASTATUS; // idecntrl = $17 (+$40 = read, +$20 = write)
#pragma warning restore CS0414 

        /* IDE STATUS BITS
        bit 0    : error bit.If this bit is set then an error has
           occurred while executing the latest command. The error
           status itself is to be found in the error register.
        bit 1    : index pulse. Each revolution of the disk this bit is
           pulsed to '1' once.I have never looked at this bit, I
           do not even know if that really happens.
        bit 2    : ECC bit. if this bit is set then an ECC correction on
           the data was executed. I ignore this bit.
        bit 3    : DRQ bit. If this bit is set then the disk either wants
           data (disk write) or has data for you (disk read).
        bit 4    : SKC bit. Indicates that a seek has been executed with
           success.I ignore this bit.
        bit 5    : WFT bit. indicates a write error has happened. I do
           not know what to do with this bit here and now. I've
           never seen it go active.
        bit 6    : RDY bit. indicates that the disk has finished its
           power-up.Wait for this bit to be active before doing
           anything (execpt reset) with the disk. I once ignored
           this bit and was rewarded with a completely unusable
           disk.
        bit 7    : BSY bit. This bit is set when the disk is doing
           something for you.You have to wait for this bit to
           clear before you can start giving orders to the disk.
        */

        private uint ideBufferCounter;
        const uint size = 4;  //4 registers
        private uint baseAddress;   //set in constructor
        private bool supports16bit = false;
        public bool SuspendLogging = false;

        private byte[] ideBuffer = new byte[513];

        public enum REGISTERS : byte
        {
            PPIDELO = 0x00,
            PPIDEHI = 0x01,
            PPIDECNTRL = 0x02,
            PPIDEPPIC = 0x03
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
                return new byte[4] { 0xff, 0xff, 0xff, 0xff };
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
                    case REGISTERS.PPIDELO:
                        return idelo;
                    case REGISTERS.PPIDEHI:
                        return idehi;
                    case REGISTERS.PPIDECNTRL:
                        // read of this register is invalid
                        break;
                    case REGISTERS.PPIDEPPIC:
                        // read of this register is invalid
                        break;
                }
                return 0x00;
            }
            set
            {
                REGISTERS register = (REGISTERS)(index - baseAddress);

                switch (register)
                {
                    case REGISTERS.PPIDELO:
                        idelo = value;
                        break;
                    case REGISTERS.PPIDEHI:
                        idehi = value;
                        break;
                    case REGISTERS.PPIDECNTRL:
                        processIdeControl(value);
                        break;
                    case REGISTERS.PPIDEPPIC:
                        ideppic = value;
                        break;

                }
            }
        }

        public void Update(bool dataChanged)
        {
            PPIDEOutChangedEventArgs eventArgs = new();
            eventArgs.DataChanged = dataChanged;
            OnPPIDEOutChanged(eventArgs);
        }

        public PPIDE(uint address, string imageFile1, string imageFile2)
        {
            baseAddress = address;
            idelo = 0x00;
            idehi = 0x00;
            idecntrl = 0x00;
            ideppic = 0x00;

            try
            {
                if (ideImage1 != null) ideImage1.Close();
            }
            catch { }

            try
            {
                if (ideImage2 != null) ideImage2.Close();
            }
            catch { }


            try
            {
                if ((imageFile1 ?? "").Length > 0)
                {
                    ideImage1 = new FileStream(imageFile1, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite);
                }
                else
                {
                    ideImage1 = null;
                }
                if ((imageFile2 ?? "").Length > 0)
                {
                    ideImage2 = new FileStream(imageFile2, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite);
                }
                else
                {
                    ideImage2 = null;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error");
            }
        }
        public void reOpen(string imageFile1, string imageFile2)
        {
            try
            {
                if (ideImage1 != null) ideImage1.Close();
            }
            catch { }

            try
            {
                if (ideImage2 != null) ideImage2.Close();
            }
            catch { }

            try
            {
                if (imageFile1.Length > 0)
                {
                    ideImage1 = new FileStream(imageFile1, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite);
                }
                else
                {
                    ideImage1 = null;
                }
                if (imageFile2.Length > 0)
                {
                    ideImage2 = new FileStream(imageFile2, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite);
                }
                else
                {
                    ideImage2 = null;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error");
            }
        }

        private void processIdeReset()
        {
            idelo = 0;
            idehi = 0;
            PPIDE_STATUS = 0b01000000;
            PPIDE_DATA = 0;
            PPIDE_ERR = 0;
            PPIDE_SEC_CNT = 1;
            PPIDE_LBALOW = 1;
            PPIDE_LBAMID = 0;
            PPIDE_LBAHI = 0;
            PPIDE_DEVICE = 0;
            PPIDE_COMMAND = 0;
            PPIDE_CONTROL = 0;
            PPIDE_ASTATUS = 0;

            ideBufferCounter = 0;

        }

        public void Close()
        {
            try
            {
                ideImage1.Close();
                ideImage2.Close();
            }
            catch
            { }
        }

        private void processIdeDataRead()
        {
            if (ideBufferCounter < 512) idelo = ideBuffer[ideBufferCounter++];
            if (ideBufferCounter < 512) idehi = ideBuffer[ideBufferCounter];
            if (ideBufferCounter > 511)
            {
                ideBufferCounter = 0;
                PPIDE_STATUS &= 0b11110111;
            }
        }
        private void processIdeDataWrite()
        {

            if (PPIDE_DEVICE == 0xE0)
            {
                ideImage1.WriteByte(idelo);
                ideImage1.WriteByte(idehi);
            }
            if (PPIDE_DEVICE == 0xF0)
            {
                ideImage2.WriteByte(idelo);
                ideImage2.WriteByte(idehi);
            }

            ideBufferCounter += 2;
            if (ideBufferCounter > 511)
            {
                ideBufferCounter = 0;
                PPIDE_STATUS &= 0b11110111;
                ideImage1.Flush();
                ideImage2.Flush();
            }
        }
        private void processIdeSecCountRead()
        {
            idelo = PPIDE_SEC_CNT;
            idehi = 0;
        }
        private void processIdeSecCountWrite()
        {
            PPIDE_SEC_CNT = idelo;
        }
        private void processIdeLbaLowRead()
        {
            idelo = PPIDE_LBALOW;
            idehi = 0;
        }
        private void processIdeLbaLowWrite()
        {
            PPIDE_LBALOW = idelo;
        }
        private void processIdeLbaMidRead()
        {
            idelo = PPIDE_LBAMID;
            idehi = 0;
        }
        private void processIdeLbaMidWrite()
        {
            PPIDE_LBAMID = idelo;
        }
        private void processIdeLbaHiRead()
        {
            idelo = PPIDE_LBAHI;
            idehi = 0;
        }
        private void processIdeLbaHiWrite()
        {
            PPIDE_LBAHI = idelo;
        }
        private void processIdeDeviceWrite()
        {
            PPIDE_DEVICE = idelo;
        }
        private void processIdeCommandWrite()
        {
            /*
             *  IDE COMMANDS
             *  PPIDE_CMD_RECAL     = 0x10
             *  PPIDE_CMD_READ      = 0x20
             *  PPIDE_CMD_WRITE     = 0x30
             *  PPIDE_CMD_INIT      = 0x91
             *  PPIDE_CMD_ID        = 0xEC
             *  PPIDE_CMD_SPINDOWN  = 0xE0
             *  PPIDE_CMD_SPINUP    = 0xE1
             */

            PPIDE_COMMAND = idelo;

            switch (idelo)
            {
                case 0x20:
                    doIdeRead();
                    break;
                case 0x30:
                    doIdeWrite();
                    break;
                case 0xEC:
                    doIdeId();
                    break;
            }
        }
        private void processIdeStatusRead()
        {
            idelo = PPIDE_STATUS;
            idehi = 0;
        }

        private void processIdeControl(byte value)
        {
            idecntrl = value;

            if ((value & 0x80) != 0) processIdeReset();

            if (value == 0x48) processIdeDataRead();
            if (value == 0x28) processIdeDataWrite();

            if (value == 0x4A) processIdeSecCountRead();
            if (value == 0x2A) processIdeSecCountWrite();

            if (value == 0x4B) processIdeLbaLowRead();
            if (value == 0x2B) processIdeLbaLowWrite();

            if (value == 0x4C) processIdeLbaMidRead();
            if (value == 0x2C) processIdeLbaMidWrite();

            if (value == 0x4D) processIdeLbaHiRead();
            if (value == 0x2D) processIdeLbaHiWrite();

            if (value == 0x2E) processIdeDeviceWrite();

            if (value == 0x2F) processIdeCommandWrite();

            if (value == 0x4F) processIdeStatusRead();
        }

        private void doIdeRead()
        {
            PPIDE_STATUS &= 0b00111111;
            ideBufferCounter = 0;

            if (PPIDE_DEVICE == 0xE0)
            {
                if(ideImage1!=null)
                    ideImage1.Position = 512 * ((PPIDE_LBAHI * 65536) + (PPIDE_LBAMID * 256) + PPIDE_LBALOW);
            }
            if (PPIDE_DEVICE == 0xF0)
            {
                if (ideImage2 != null)
                    ideImage2.Position = 512 * ((PPIDE_LBAHI * 65536) + (PPIDE_LBAMID * 256) + PPIDE_LBALOW);
            }

            for (int i = 0; i < 512; i++)
            {
                if (PPIDE_DEVICE == 0xE0)
                {
                    if (ideImage1 != null)
                        ideBuffer[i] = (byte)ideImage1.ReadByte();
                }
                if (PPIDE_DEVICE == 0xF0)
                {
                    if (ideImage2 != null)
                        ideBuffer[i] = (byte)ideImage2.ReadByte();
                }
            }


            PPIDE_STATUS |= 0b01001000;
        }
        private void doIdeWrite()
        {
            PPIDE_STATUS &= 0b00111111;
            ideBufferCounter = 0;

            if (PPIDE_DEVICE == 0xE0)
            {
                ideImage1.Position = 512 * ((PPIDE_LBAHI * 65536) + (PPIDE_LBAMID * 256) + PPIDE_LBALOW);
            }
            if (PPIDE_DEVICE == 0xF0)
            {
                ideImage2.Position = 512 * ((PPIDE_LBAHI * 65536) + (PPIDE_LBAMID * 256) + PPIDE_LBALOW);
            }

            for (int i = 0; i < 512; i++)
            {
                if (PPIDE_DEVICE == 0xE0)
                {
                    ideBuffer[i] = (byte)ideImage1.ReadByte();
                }
                if (PPIDE_DEVICE == 0xF0)
                {
                    ideBuffer[i] = (byte)ideImage2.ReadByte();
                }
            }

            if (PPIDE_DEVICE == 0xE0)
            {
                ideImage1.Position = 512 * ((PPIDE_LBAHI * 65536) + (PPIDE_LBAMID * 256) + PPIDE_LBALOW);
            }
            if (PPIDE_DEVICE == 0xF0)
            {
                ideImage2.Position = 512 * ((PPIDE_LBAHI * 65536) + (PPIDE_LBAMID * 256) + PPIDE_LBALOW);
            }


            PPIDE_STATUS |= 0b01001000;
        }
        private void doIdeId()
        {
            PPIDE_STATUS &= 0b00111111;
            ideBufferCounter = 0;

            for (int i = 0; i < 512; i++) ideBuffer[i] = 0;

            if (PPIDE_DEVICE == 0xE0)
            {
                ideBuffer[123] = 0;
                ideBuffer[122] = 0;
                ideBuffer[121] = 0;
                ideBuffer[120] = 0;
                if (ideImage1 != null)
                {
                    ideBuffer[122] = GetByte(ideImage1.Length, 2);
                    ideBuffer[121] = GetByte(ideImage1.Length, 1);
                    ideBuffer[120] = GetByte(ideImage1.Length, 0);
                }
            }
            if (PPIDE_DEVICE == 0xF0)
            {
                ideBuffer[123] = 0;
                ideBuffer[122] = 0;
                ideBuffer[121] = 0;
                ideBuffer[120] = 0;
                if (ideImage2 != null)
                {
                    ideBuffer[122] = GetByte(ideImage2.Length, 2);
                    ideBuffer[121] = GetByte(ideImage2.Length, 1);
                    ideBuffer[120] = GetByte(ideImage2.Length, 0);
                }
            }
            PPIDE_STATUS |= 0b01001000;
        }


        private byte GetByte(long len, byte byteNum)
        {
            len = len / 512;

            switch (byteNum)
            {
                case 2:
                    return (byte)(len >> 16);
                case 1:
                    return (byte)(len >> 8);
                case 0:
                    return (byte)(len);
            }

            return 0;
        }

    }
}
