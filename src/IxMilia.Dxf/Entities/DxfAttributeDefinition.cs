// Copyright (c) IxMilia.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Diagnostics;

namespace IxMilia.Dxf.Entities
{
    public partial class DxfAttributeDefinition
    {
        private const string AcDbXrecordText = "AcDbXrecord";
        private string _lastSubclassMarker;
        private bool _isVersionSet;
        private int _xrecCode70Count = 0;

        internal override bool TrySetPair(DxfCodePair pair)
        {
            switch (pair.Code)
            {
                case 100:
                    _lastSubclassMarker = pair.StringValue;
                    break;
                case 1:
                    Value = pair.StringValue;
                    break;
                case 2:
                    if (_lastSubclassMarker == AcDbXrecordText) XRecordTag = pair.StringValue;
                    else TextTag = pair.StringValue;
                    break;
                case 3:
                    Prompt = pair.StringValue;
                    break;
                case 7:
                    TextStyleName = pair.StringValue;
                    break;
                case 10:
                    if (_lastSubclassMarker == AcDbXrecordText) AlignmentPoint.X = pair.DoubleValue;
                    else Location.X = pair.DoubleValue;
                    break;
                case 20:
                    if (_lastSubclassMarker == AcDbXrecordText) AlignmentPoint.Y = pair.DoubleValue;
                    else Location.Y = pair.DoubleValue;
                    break;
                case 30:
                    if (_lastSubclassMarker == AcDbXrecordText) AlignmentPoint.Z = pair.DoubleValue;
                    else Location.Z = pair.DoubleValue;
                    break;
                case 11:
                    SecondAlignmentPoint.X = pair.DoubleValue;
                    break;
                case 21:
                    SecondAlignmentPoint.Y = pair.DoubleValue;
                    break;
                case 31:
                    SecondAlignmentPoint.Z = pair.DoubleValue;
                    break;
                case 39:
                    Thickness = pair.DoubleValue;
                    break;
                case 40:
                    if (_lastSubclassMarker == AcDbXrecordText) AnnotationScale = pair.DoubleValue;
                    else TextHeight = pair.DoubleValue;
                    break;
                case 41:
                    RelativeXScaleFactor = pair.DoubleValue;
                    break;
                case 50:
                    Rotation = pair.DoubleValue;
                    break;
                case 51:
                    ObliqueAngle = pair.DoubleValue;
                    break;
                case 70:
                    if (_lastSubclassMarker == AcDbXrecordText)
                    {
                        switch (_xrecCode70Count)
                        {
                            case 0:
                                MTextFlag = (DxfMTextFlag)pair.ShortValue;
                                break;
                            case 1:
                                IsReallyLocked = BoolShort(pair.ShortValue);
                                break;
                            case 2:
                                _secondaryAttributeCount = pair.ShortValue;
                                break;
                            default:
                                Debug.Assert(false, "Unexpected extra values");
                                break;
                        }

                        _xrecCode70Count++;
                    }
                    else
                    {
                        Flags = pair.ShortValue;
                    }
                    break;
                case 71:
                    TextGenerationFlags = pair.ShortValue;
                    break;
                case 72:
                    HorizontalTextJustification = (DxfHorizontalTextJustification)pair.ShortValue;
                    break;
                case 73:
                    FieldLength = pair.ShortValue;
                    break;
                case 74:
                    VerticalTextJustification = (DxfVerticalTextJustification)pair.ShortValue;
                    break;
                case 210:
                    Normal.X = pair.DoubleValue;
                    break;
                case 220:
                    Normal.Y = pair.DoubleValue;
                    break;
                case 230:
                    Normal.Z = pair.DoubleValue;
                    break;
                case 280:
                    if (_lastSubclassMarker == AcDbXrecordText) KeepDuplicateRecords = BoolShort(pair.ShortValue);
                    else if (!_isVersionSet)
                    {
                        Version = (DxfVersion)pair.ShortValue;
                        _isVersionSet = true;
                    }
                    else IsLockedInBlock = BoolShort(pair.ShortValue);
                    break;
                case 340:
                    SecondaryAttributesPointers.Pointers.Add(new DxfPointer(UIntHandle(pair.StringValue)));
                    break;
                default:
                    return base.TrySetPair(pair);
            }

            return true;
        }

        protected override void AddTrailingCodePairs(List<DxfCodePair> pairs, DxfAcadVersion version, bool outputHandles)
        {
            if (MText != null)
            {
                pairs.AddRange(MText.GetValuePairs(version, outputHandles));
            }
        }
    }
}
