using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace IxMilia.Dxf.Objects
{
    public partial class DxfDataTable
    {
        private bool _readRowCount = false;
        private bool _readColumnCount = false;
        private bool _createdTable = false;
        private int _currentColumnCode = 0;
        private int _currentColumn = -1;
        private int _currentRow = -1;
        private DxfPoint _current2DPoint;
        private DxfPoint _current3DPoint;

        public object[,] Values { get; private set; } = new object[0, 0];

        public object this[int row, int column]
        {
            get { return Values[row, column]; }
            set { Values[row, column] = value; }
        }

        public void SetSize(int rows, int columns)
        {
            if (rows < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(rows));
            }

            if (columns < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(columns));
            }

            var newValues = new object[rows, columns];
            for (int row = 0; row < Math.Min(rows, RowCount); row++)
            {
                for (int col = 0; col < Math.Min(columns, ColumnCount); col++)
                {
                    newValues[row, col] = Values[row, col];
                }
            }

            Values = newValues;
            RowCount = rows;
            ColumnCount = columns;
        }

        protected override void AddValuePairs(List<DxfCodePair> pairs, DxfAcadVersion version, bool outputHandles, bool writeXData)
        {
            base.AddValuePairs(pairs, version, outputHandles, writeXData: false);
            pairs.Add(new DxfCodePair(100, "AcDbDataTable"));
            pairs.Add(new DxfCodePair(70, (this.Field)));
            pairs.Add(new DxfCodePair(90, (this.ColumnCount)));
            pairs.Add(new DxfCodePair(91, (this.RowCount)));
            pairs.Add(new DxfCodePair(1, (this.Name)));

            for (int col = 0; col < ColumnCount; col++)
            {
                var columnCode = GetCodeFromColumnType(col);
                pairs.Add(new DxfCodePair(92, columnCode));
                pairs.Add(new DxfCodePair(2, ColumnNames[col]));
                for (int row = 0; row < RowCount; row++)
                {
                    pairs.AddRange(GeneratePairsFromCode(columnCode, Values[row, col]));
                }
            }

            if (writeXData)
            {
                DxfXData.AddValuePairs(XData, pairs, version, outputHandles);
            }
        }

        internal override bool TrySetPair(DxfCodePair pair)
        {
            switch (pair.Code)
            {
                case 1:
                    this.Name = (pair.StringValue);
                    break;
                case 70:
                    this.Field = (pair.ShortValue);
                    break;
                case 90:
                    this.ColumnCount = (pair.IntegerValue);
                    _readColumnCount = true;
                    break;
                case 91:
                    this.RowCount = (pair.IntegerValue);
                    _readRowCount = true;
                    break;

                // column headers
                case 92:
                    _currentColumnCode = pair.IntegerValue;
                    _currentColumn++;
                    _currentRow = 0;
                    break;
                case 2:
                    this.ColumnNames.Add(pair.StringValue);
                    break;

                // column values
                case 71:
                    SetValue(BoolShort(pair.ShortValue));
                    break;
                case 93:
                    SetValue(pair.IntegerValue);
                    break;
                case 40:
                    SetValue(pair.DoubleValue);
                    break;
                case 3:
                    SetValue(pair.StringValue);
                    break;
                case 10:
                    _current2DPoint = new DxfPoint(pair.DoubleValue, 0.0, 0.0);
                    break;
                case 20:
                    _current2DPoint = _current2DPoint.WithUpdatedY(pair.DoubleValue);
                    break;
                case 30:
                    _current2DPoint = _current2DPoint.WithUpdatedZ(pair.DoubleValue);
                    SetValue(_current2DPoint);
                    _current2DPoint = default(DxfPoint);
                    break;
                case 11:
                    _current3DPoint = new DxfPoint(pair.DoubleValue, 0.0, 0.0);
                    break;
                case 21:
                    _current3DPoint = _current3DPoint.WithUpdatedY(pair.DoubleValue);
                    break;
                case 31:
                    _current3DPoint = _current3DPoint.WithUpdatedZ(pair.DoubleValue);
                    SetValue(_current3DPoint);
                    _current3DPoint = default(DxfPoint);
                    break;
                case 331:
                case 360:
                case 350:
                case 340:
                case 330:
                    if (_readRowCount || _readColumnCount)
                    {
                        // TODO: differentiate between handle types
                        SetValue(HandleString(pair.StringValue));
                    }
                    else
                    {
                        // still reading AcDbObject values
                        goto default;
                    }
                    break;

                default:
                    return base.TrySetPair(pair);
            }

            if (_readRowCount && _readColumnCount && !_createdTable)
            {
                Values = new object[RowCount, ColumnCount];
                _createdTable = true;
            }

            return true;
        }

        private void SetValue(object value)
        {
            if (_currentRow < 0 || _currentRow >= RowCount)
            {
                Debug.Assert(false, "Row out of range");
                return;
            }

            if (_currentColumn < 0 || _currentColumn >= ColumnCount)
            {
                Debug.Assert(false, "Column out of range");
                return;
            }

            Values[_currentRow, _currentColumn] = value;
            _currentRow++;
        }

        private int GetCodeFromColumnType(int column)
        {
            var value = Values[0, column];
            if (value.GetType() == typeof(bool))
            {
                return 71;
            }
            else if (value.GetType() == typeof(int))
            {
                return 93;
            }
            else if (value.GetType() == typeof(double))
            {
                return 40;
            }
            else if (value.GetType() == typeof(string))
            {
                return 3;
            }
            else if (value.GetType() == typeof(DxfPoint))
            {
                // TODO: how to differentiate between 2D and 3D point?
                return 10;
            }
            else if (value.GetType() == typeof(DxfHandle))
            {
                // TODO: differentiate between handle types
                return 331;
            }
            else
            {
                throw new InvalidOperationException("Unsupported column type: " + value.GetType().Name);
            }
        }

        private IEnumerable<DxfCodePair> GeneratePairsFromCode(int code, object value)
        {
            var expectedType = DxfCodePair.ExpectedType(code);
            if (expectedType == typeof(bool))
            {
                return new[] { new DxfCodePair(code, BoolShort((bool)value)) };
            }
            else if (expectedType == typeof(int))
            {
                return new[] { new DxfCodePair(code, (int)value) };
            }
            else if (code == 40)
            {
                return new[] { new DxfCodePair(code, (double)value) };
            }
            else if (expectedType == typeof(string))
            {
                return new[] { new DxfCodePair(code, (string)value) };
            }
            else if (code == 10 || code == 11)
            {
                var point = (DxfPoint)value;
                return new[]
                {
                    new DxfCodePair(code, point.X),
                    new DxfCodePair(code + 10, point.Y),
                    new DxfCodePair(code + 20, point.Z),
                };
            }
            else if (expectedType == typeof(string) && code >= 330)
            {
                // TODO: differentiate between handle types
                return new[] { new DxfCodePair(code, HandleString((DxfHandle)value)) };
            }
            else
            {
                throw new ArgumentOutOfRangeException(nameof(code));
            }
        }
    }
}
