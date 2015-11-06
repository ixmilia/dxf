// Copyright (c) IxMilia.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace IxMilia.Dxf.Objects
{
    public enum DxfPlotPaperUnits
    {
        Inches = 0,
        Millimeters = 1,
        Pixels = 2
    }

    public enum DxfPlotRotation
    {
        NoRotation = 0,
        CounterClockwise90Degrees = 1,
        UpsideDown = 2,
        Clockwise90Degrees = 3
    }

    public enum DxfPlotType
    {
        LastScreenDisplay = 0,
        DrawingExtents = 1,
        DrawingLimits = 2,
        SpecificView = 3,
        SpecificWindow = 4,
        LayoutInformation = 5
    }

    public enum DxfStandardScale
    {
        ScaledToFit = 0,
        Scale_1_128_inch_to_1_foot = 1,
        Scale_1_64_inch_to_1_foot = 2,
        Scale_1_32_inch_to_1_foot = 3,
        Scale_1_16_inch_to_1_foot = 4,
        Scale_3_32_inch_to_1_foot = 5,
        Scale_1_8_inch_to_1_foot = 6,
        Scale_3_16_inch_to_1_foot = 7,
        Scale_1_4_inch_to_1_foot = 8,
        Scale_3_8_inch_to_1_foot = 9,
        Scale_1_2_inch_to_1_foot = 10,
        Scale_3_4_inch_to_1_foot = 11,
        Scale_1_inch_to_1_foot = 12,
        Scale_3_inches_to_1_foot = 13,
        Scale_6_inches_to_1_foot = 14,
        Scale_1_foot_to_1_foot = 15,
        Scale_1_to_1 = 16,
        Scale_1_to_2 = 17,
        Scale_1_to_4 = 18,
        Scale_1_to_8 = 19,
        Scale_1_to_10 = 20,
        Scale_1_to_16 = 21,
        Scale_1_to_20 = 22,
        Scale_1_to_30 = 23,
        Scale_1_to_40 = 24,
        Scale_1_to_50 = 25,
        Scale_1_to_100 = 26,
        Scale_2_to_1 = 27,
        Scale_4_to_1 = 28,
        Scale_8_to_1 = 29,
        Scale_10_to_1 = 30,
        Scale_100_to_1 = 31,
        Scale_1000_to_1 = 32
    }

    public enum DxfShadePlotMode
    {
        AsDisplayed = 0,
        Wireframe = 1,
        Hidden = 2,
        Rendered = 3
    }

    public enum DxfShadePlotResolutionLevel
    {
        Draft = 0,
        Preview = 1,
        Normal = 2,
        Presentation = 3,
        Maximum = 4,
        Custom = 5
    }

    public partial class DxfPlotSettings
    {
    }
}
