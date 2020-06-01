namespace IxMilia.Dxf
{
    internal static class DxfHelpers
    {
        public static void SetFlag(ref int flags, int mask)
        {
            flags |= mask;
        }

        public static void ClearFlag(ref int flags, int mask)
        {
            flags &= ~mask;
        }

        public static bool GetFlag(int flags, int mask)
        {
            return (flags & mask) != 0;
        }

        public static void SetFlag(bool value, ref int flags, int mask)
        {
            if (value) SetFlag(ref flags, mask);
            else ClearFlag(ref flags, mask);
        }
    }
}
