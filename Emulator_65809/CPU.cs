using System;
using static Emul809or.CPU;
using System.Windows.Forms.VisualStyles;

namespace Emul809or
{
    enum OffsetRegisterIndex
    {
        m_X = 0,
        m_Y = 1,
        m_U = 2,
        m_S = 3
    }

    public class StatusChangedEventArgs : EventArgs
    {
        public byte A;
        public byte B;
        public ushort X;
        public ushort Y;
        public ushort U;
        public ushort S;
        public byte DP;
        public byte CCR;
        public ushort PC;
        public long Cycles;


        public bool FlagE
        {
            get
            {
                if ((CCR & 0b10000000) == 0) return false;
                return true;
            }
        }
        public bool FlagF
        {
            get
            {
                if ((CCR & 0b01000000) == 0) return false;
                return true;
            }
        }
        public bool FlagH
        {
            get
            {
                if ((CCR & 0b00100000) == 0) return false;
                return true;
            }
        }
        public bool FlagI
        {
            get
            {
                if ((CCR & 0b00010000) == 0) return false;
                return true;
            }
        }
        public bool FlagN
        {
            get
            {
                if ((CCR & 0b00001000) == 0) return false;
                return true;
            }
        }
        public bool FlagZ
        {
            get
            {
                if ((CCR & 0b00000100) == 0) return false;
                return true;
            }
        }
        public bool FlagV
        {
            get
            {
                if ((CCR & 0b00000010) == 0) return false;
                return true;
            }
        }
        public bool FlagC
        {
            get
            {
                if ((CCR & 0b00000001) == 0) return false;
                return true;
            }
        }
    }
    public class LogTextUpdateEventArgs : EventArgs
    {
        public string NewText;
    }

    public class BreakEventArgs : EventArgs
    {
        public string Data;
    }

    public class TraceEntry
    {
        public ushort sExecutionTrace;
        public ushort sStackTrace;
        public byte cTable;
        public byte cOpcode;
        public byte cPostByte;
        public ushort sOperand;
        public ushort sValue;
        public byte cRegisterDAT;
        public byte cRegisterOperandDAT;
        public byte cRegisterDP;
        public byte cRegisterA;
        public byte cRegisterB;
        public ushort sRegisterX;
        public ushort sRegisterY;
        public ushort sRegisterU;
        public byte cRegisterCCR;
        public bool sRegisterInterrupts;
        public uint nPostIncerment;
        public uint nPreDecrement;
    }



}
