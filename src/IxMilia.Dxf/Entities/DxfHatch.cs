// Copyright (c) IxMilia.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;

namespace IxMilia.Dxf.Entities
{
    public partial class DxfHatch
    {
        private enum ReadMode
        {
            Entity,
            BoundaryPath,
            PatternData,
            SeedPoints,
        }

        private ReadMode _readMode = ReadMode.Entity;

        internal override bool TrySetPair(DxfCodePair pair)
        {
            switch (_readMode)
            {
                case ReadMode.BoundaryPath:
                    if (pair.Code == 92)
                    {
                        // new boundary path
                        var pathType = (BoundaryPathType)pair.IntegerValue;
                        BoundaryPaths.Add(BoundaryPathBase.CreateFromType(pathType));
                        return true;
                    }

                    if (BoundaryPaths.Last().TrySetPair(pair))
                    {
                        return true;
                    }
                    else
                    {
                        _readMode = ReadMode.Entity;
                        break;
                    }
                case ReadMode.PatternData:
                    if (pair.Code == 53)
                    {
                        // new pattern definition line
                        PatternDefinitionLines.Add(new PatternDefinitionLine());
                    }

                    if (PatternDefinitionLines.Last().TrySetPair(pair))
                    {
                        return true;
                    }
                    else
                    {
                        _readMode = ReadMode.Entity;
                        break;
                    }
                case ReadMode.SeedPoints:
                    switch (pair.Code)
                    {
                        case 10:
                            // new seed point
                            SeedPoints.Add(new DxfPoint(pair.DoubleValue, 0.0, 0.0));
                            return true;
                        case 20:
                            SeedPoints[SeedPoints.Count - 1] = SeedPoints.Last().WithUpdatedY(pair.DoubleValue);
                            return true;
                        default:
                            _readMode = ReadMode.Entity;
                            break;
                    }
                    break;
            }

            switch (pair.Code)
            {
                case 2:
                    this.PatternName = pair.StringValue;
                    break;
                case 10:
                    this.ElevationPoint = this.ElevationPoint.WithUpdatedX(pair.DoubleValue);
                    break;
                case 20:
                    this.ElevationPoint = this.ElevationPoint.WithUpdatedY(pair.DoubleValue);
                    break;
                case 30:
                    this.ElevationPoint = this.ElevationPoint.WithUpdatedZ(pair.DoubleValue);
                    break;
                case 41:
                    this.PatternScale = pair.DoubleValue;
                    break;
                case 47:
                    this.PixelSize = pair.DoubleValue;
                    break;
                case 52:
                    this.PatternAngle = pair.DoubleValue;
                    break;
                case 63:
                    this.FillColor = DxfColor.FromRawValue(pair.ShortValue);
                    break;
                case 70:
                    this.FillMode = (DxfHatchPatternFillMode)pair.ShortValue;
                    break;
                case 71:
                    this.IsAssociative = BoolShort(pair.ShortValue);
                    break;
                case 75:
                    this.HatchStyle = (DxfHatchStyle)pair.ShortValue;
                    break;
                case 76:
                    this.PatternType = (DxfHatchPatternType)pair.ShortValue;
                    break;
                case 77:
                    this.IsPatternDoubled = BoolShort(pair.ShortValue);
                    break;
                case 78:
                    this._patternDefinitionLineCount = pair.ShortValue;
                    _readMode = ReadMode.PatternData;
                    break;
                case 91:
                    this._boundaryPathCount = pair.IntegerValue;
                    _readMode = ReadMode.BoundaryPath;
                    break;
                case 98:
                    this._seedPointCount = pair.IntegerValue;
                    _readMode = ReadMode.SeedPoints;
                    break;
                case 210:
                    this.ExtrusionDirection = this.ExtrusionDirection.WithUpdatedX(pair.DoubleValue);
                    break;
                case 220:
                    this.ExtrusionDirection = this.ExtrusionDirection.WithUpdatedY(pair.DoubleValue);
                    break;
                case 230:
                    this.ExtrusionDirection = this.ExtrusionDirection.WithUpdatedZ(pair.DoubleValue);
                    break;
                case 450:
                    this.IsGradient = BoolLong(pair.LongValue);
                    break;
                case 451:
                    this._zero = pair.LongValue;
                    break;
                case 452:
                    this.GradientColorMode = (DxfGradientColorMode)pair.LongValue;
                    break;
                case 453:
                    this.NumberOfColors = pair.LongValue;
                    break;
                case 460:
                    this.GradientRotationAngle = pair.DoubleValue;
                    break;
                case 461:
                    this.GradientDefinitionShift = pair.DoubleValue;
                    break;
                case 462:
                    this.ColorTint = pair.DoubleValue;
                    break;
                case 463:
                    this._reserved = pair.DoubleValue;
                    break;
                case 470:
                    this.StringValue = pair.StringValue;
                    break;
                default:
                    return base.TrySetPair(pair);
            }

            return true;
        }
    }
}
