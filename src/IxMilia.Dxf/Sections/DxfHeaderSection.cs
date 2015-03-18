// Copyright (c) IxMilia.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace IxMilia.Dxf
{
    public enum DxfUnitFormat
    {
        Scientific = 1,
        Decimal = 2,
        Engineering = 3,
        ArchitecturalStacked = 4,
        FractionalStacked = 5,
        Architectural = 6,
        Fractional = 7,
    }

    public enum DxfAngleDirection
    {
        CounterClockwise = 0,
        Clockwise = 1
    }

    public enum DxfAttributeVisibility
    {
        None = 0,
        Normal = 1,
        All = 2
    }

    public enum DxfJustification
    {
        Top = 0,
        Middle = 1,
        Bottom = 2
    }

    public enum DxfDimensionTextJustification
    {
        AboveLineCenter = 0,
        AboveLineNextToFirstExtension = 1,
        AboveLineNextToSecondExtension = 2,
        AboveLineCenteredOnFirstExtension = 3,
        AboveLineCenteredOnSecondExtension = 4
    }

    public enum DxfCoordinateDisplay
    {
        Static = 0,
        ContinuousUpdate = 1,
        DistanceAngleFormat = 2
    }

    public enum DxfUnitZeroSuppression
    {
        SuppressZeroFeetAndZeroInches = 0,
        IncludeZeroFeetAndZeroInches = 1,
        IncludeZeroFeetAndSuppressZeroInches = 2,
        IncludeZeroInchesAndSuppressZeroFeet = 3
    }

    public enum DxfAngleFormat
    {
        DecimalDegrees = 0,
        DegreesMinutesSeconds = 1,
        Gradians = 2,
        Radians = 3,
        SurveyorsUnits = 4
    }

    public enum DxfDimensionFit
    {
        TextAndArrowsOutsideLines = 0,
        MoveArrowsFirst = 1,
        MoveTextFirst = 2,
        MoveEitherForBestFit = 3
    }

    public enum DxfDragMode
    {
        Off = 0,
        On = 1,
        Auto = 2
    }

    public enum DxfDrawingUnits
    {
        English = 0,
        Metric = 1
    }

    public enum DxfPickStyle
    {
        None = 0,
        Group = 1,
        AssociativeHatch = 2,
        GroupAndAssociativeHatch = 3
    }

    public enum DxfShadeEdgeMode
    {
        FacesShadedEdgeNotHighlighted = 0,
        FacesShadedEdgesHighlightedInBlack = 1,
        FacesNotFilledEdgesInEntityColor = 2,
        FacesInEntityColorEdgesInBlack = 3
    }

    public enum DxfPolySketchMode
    {
        SketchLines = 0,
        SketchPolylines = 1
    }

    public enum DxfPlotStyle
    {
        ByLayer = 0,
        ByBlock = 1,
        ByDictionaryDefault = 2,
        ByObjectId = 3
    }

    public enum DxfDimensionAssociativity
    {
        NoAssociationExploded = 0,
        NonAssociativeObjects = 1,
        AssociativeObjects = 2
    }

    public enum DxfNonAngularUnits
    {
        Scientific = 1,
        Decimal = 2,
        Engineering = 3,
        Architectural = 4,
        Fractional = 5,
        WindowsDesktop = 6
    }

    public enum DxfDimensionTextMovementRule
    {
        MoveLineWithText = 0,
        AddLeaderWhenTextIsMoved = 1,
        MoveTextFreely = 2
    }

    public enum DxfEndCapSetting
    {
        None = 0,
        Round = 1,
        Angle = 2,
        Square = 3
    }

    public enum DxfLayerAndSpatialIndexSaveMode
    {
        None = 0,
        LayerIndex = 1,
        SpatialIndex = 2,
        LayerAndSpatialIndex = 3
    }

    public enum DxfUnits
    {
        Unitless = 0,
        Inches = 1,
        Feet = 2,
        Miles = 3,
        Millimeters = 4,
        Centimeters = 5,
        Meters = 6,
        Kilometers = 7,
        Microinches = 8,
        Mils = 9,
        Yards = 10,
        Angstroms = 11,
        Nanometers = 12,
        Microns = 13,
        Decimeters = 14,
        Decameters = 15,
        Hectometers = 16,
        Gigameters = 17,
        AstronomicalUnits = 18,
        LightYears = 19,
        Parsecs = 20
    }

    public enum DxfJoinStyle
    {
        None = 0,
        Round = 1,
        Angle = 2,
        Flat = 3
    }

    public enum DxfLinetypeStyle
    {
        Off = 0,
        Solid = 1,
        Dashed = 2,
        Dotted = 3,
        ShortDash = 4,
        MediumDash = 5,
        LongDash = 6,
        DoubleShortDash = 7,
        DoubleMediumDash = 8,
        DoubleLongDash = 9,
        MediumLongDash = 10,
        SparseDot = 11
    }

    public enum DxfOrthographicViewType
    {
        None = 0,
        Top = 1,
        Bottom = 2,
        Front = 3,
        Back = 4,
        Left = 5,
        Right = 6
    }
}

namespace IxMilia.Dxf.Sections
{
    internal class DxfHeaderSection : DxfSection
    {
        public DxfHeader Header { get; private set; }

        public DxfHeaderSection()
        {
            Header = new DxfHeader();
        }

        public override DxfSectionType Type
        {
            get { return DxfSectionType.Header; }
        }

        protected internal override IEnumerable<DxfCodePair> GetSpecificPairs(DxfAcadVersion version)
        {
            var values = new List<DxfCodePair>();
            DxfHeader.AddValueToList(values, this.Header, version);
            return values;
        }

        internal static DxfHeaderSection HeaderSectionFromBuffer(DxfCodePairBufferReader buffer)
        {
            var section = new DxfHeaderSection();
            string keyName = null;
            Func<short, bool> shortToBool = value => value != 0;

            while (buffer.ItemsRemain)
            {
                var pair = buffer.Peek();
                buffer.Advance();
                if (DxfCodePair.IsSectionEnd(pair))
                {
                    // done reading settings
                    break;
                }

                if (pair.Code == 9)
                {
                    // what setting to get
                    keyName = pair.StringValue;
                }
                else
                {
                    DxfHeader.SetHeaderVariable(keyName, pair, section.Header);
                }
            }

            return section;
        }
    }
}
