using System.Diagnostics;

namespace IxMilia.Dxf.Objects
{
    public partial class DxfField
    {
        public string FormatString { get; set; }

        public object Value
        {
            get
            {
                switch (_valueTypeCode)
                {
                    case 91:
                        return _longValue;
                    case 140:
                        return _doubleValue;
                    case 330:
                        return _idValue;
                    case 310:
                        return _binaryData;
                    default:
                        return null;
                }
            }
            set
            {
                if (value == null)
                {
                    _binaryData = null;
                    _valueTypeCode = 310;
                }
                else if (value.GetType() == typeof(int))
                {
                    _longValue = (int)value;
                    _valueTypeCode = 91;
                }
                else if (value.GetType() == typeof(double))
                {
                    _doubleValue = (double)value;
                    _valueTypeCode = 140;
                }
                else if (value.GetType() == typeof(uint))
                {
                    _idValue = (uint)value;
                    _valueTypeCode = 330;
                }
                else if (value.GetType() == typeof(string))
                {
                    _binaryData = (string)value;
                    _valueTypeCode = 310;
                }
                else
                {
                    Debug.Assert(false, "Unexpected field value.");
                }
            }
        }

        protected override DxfObject PostParse()
        {
            // code 90 is shared between `_childFieldCount` and `_valueTypeCode` so they're teased apart here
            Debug.Assert(_childFieldCount_valueTypeCode.Count == 2);
            _childFieldCount = _childFieldCount_valueTypeCode[0];
            _valueTypeCode = _childFieldCount_valueTypeCode[1];
            _childFieldCount_valueTypeCode.Clear();

            // rebuild format string
            FormatString = _formatStringCode4 ?? (_formatStringCode301 + _formatStringOverflow);
            _formatStringOverflow = null;

            return this;
        }
    }
}
