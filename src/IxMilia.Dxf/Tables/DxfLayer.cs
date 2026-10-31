namespace IxMilia.Dxf
{
    public partial class DxfLayer
    {
        public bool IsFrozen
        {
            get { return DxfHelpers.GetFlag(StandardFlags, 1); }
            set { DxfHelpers.SetFlag(value, ref StandardFlags, 1); }
        }

        public bool IsFrozenByDefaultInNewViewPorts
        {
            get { return DxfHelpers.GetFlag(StandardFlags, 2); }
            set { DxfHelpers.SetFlag(value, ref StandardFlags, 2); }
        }

        public bool IsLocked
        {
            get { return DxfHelpers.GetFlag(StandardFlags, 4); }
            set { DxfHelpers.SetFlag(value, ref StandardFlags, 4); }
        }
    }
}
