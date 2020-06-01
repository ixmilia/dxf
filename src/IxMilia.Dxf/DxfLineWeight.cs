namespace IxMilia.Dxf
{
    public enum DxfLineWeightType : short
    {
        Standard = -3,
        ByLayer = -2,
        ByBlock = -1,
        Custom = 0
    }

    public class DxfLineWeight
    {
        /// <summary>
        /// A non-zero value indicates 1/100th of a mm.
        /// </summary>
        public short Value { get; set; }

        /// <summary>
        /// When the line weight type is <see cref="DxfLineWeightType.Custom" />, the <see cref="Value" /> property represents the width of the line in 1/100th mm increments.
        /// </summary>
        public DxfLineWeightType LineWeightType
        {
            get
            {
                switch (Value)
                {
                    case -3:
                        return DxfLineWeightType.Standard;
                    case -2:
                        return DxfLineWeightType.ByLayer;
                    case -1:
                        return DxfLineWeightType.ByBlock;
                    default:
                        return DxfLineWeightType.Custom;
                }
            }
        }

        public void SetStandard()
        {
            Value = (short)DxfLineWeightType.Standard;
        }

        public void SetByLayer()
        {
            Value = (short)DxfLineWeightType.ByLayer;
        }

        public void SetByBlock()
        {
            Value = (short)DxfLineWeightType.ByBlock;
        }

        public DxfLineWeight()
            : this((short)DxfLineWeightType.ByLayer)
        {
        }

        public static DxfLineWeight Standard => new DxfLineWeight((short)DxfLineWeightType.Standard);
        public static DxfLineWeight ByLayer => new DxfLineWeight((short)DxfLineWeightType.ByLayer);
        public static DxfLineWeight ByBlock => new DxfLineWeight((short)DxfLineWeightType.ByBlock);

        private DxfLineWeight(short value)
        {
            Value = value;
        }

        public static bool operator ==(DxfLineWeight a, DxfLineWeight b)
        {
            if (ReferenceEquals(a, b))
            {
                return true;
            }

            if ((object)a == null || (object)b == null)
            {
                return false;
            }

            return a.Value == b.Value;
        }

        public static bool operator !=(DxfLineWeight a, DxfLineWeight b)
        {
            return !(a == b);
        }

        public override bool Equals(object obj)
        {
            if (obj is DxfLineWeight)
            {
                return this == (DxfLineWeight)obj;
            }
            else
            {
                return false;
            }
        }

        public override int GetHashCode()
        {
            return Value.GetHashCode();
        }

        internal static DxfLineWeight FromRawValue(short value)
        {
            return new DxfLineWeight(value);
        }

        internal static short GetRawValue(DxfLineWeight lineWeight)
        {
            return lineWeight?.Value ?? 0;
        }
    }
}
