using System.Collections.Generic;

namespace IxMilia.Dxf
{
    public abstract class DxfSymbolTableFlags
    {
        protected int Flags = 0;

        public string Name { get; set; }
        protected abstract string TableType { get; }
        public string Handle { get; set; }

        public DxfSymbolTableFlags()
        {
        }

        internal void AddCommonValuePairs(List<DxfCodePair> pairs)
        {
            pairs.Add(new DxfCodePair(0, TableType));
            pairs.Add(new DxfCodePair(5, Handle));
            pairs.Add(new DxfCodePair(100, "AcDbSymbolTableRecord"));
        }

        internal abstract void AddValuePairs(List<DxfCodePair> pairs);

        public bool ExternallyDependentOnXRef
        {
            get { return DxfHelpers.GetFlag(Flags, 16); }
            set { DxfHelpers.SetFlag(value, ref Flags, 16); }
        }

        public bool ExternallyDependentXRefResolved
        {
            get { return ExternallyDependentOnXRef && DxfHelpers.GetFlag(Flags, 32); }
            set
            {
                ExternallyDependentOnXRef = true;
                DxfHelpers.SetFlag(value, ref Flags, 32);
            }
        }

        public bool ReferencedOnLastEdit
        {
            get { return DxfHelpers.GetFlag(Flags, 64); }
            set { DxfHelpers.SetFlag(value, ref Flags, 64); }
        }

        protected static bool BoolShort(short s)
        {
            return s != 0;
        }

        protected static short BoolShort(bool b)
        {
            return (short)(b ? 1 : 0);
        }
    }
}
