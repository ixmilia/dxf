// Copyright (c) IxMilia.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

namespace IxMilia.Dxf.Objects
{
    public enum DxfFaceLightingModel
    {
        Invisible = 0,
        Visible = 1,
        Phong = 2,
        Gooch = 3
    }

    public enum DxfFaceLightingQuality
    {
        None = 0,
        PerFace = 1,
        PerVertex = 2
    }

    public enum DxfFaceColorMode
    {
        NoColor = 0,
        ObjectColor = 1,
        BackgroundColor = 2,
        CustomColor = 3,
        MonoColor = 4,
        Tinted = 5,
        Desaturated = 6
    }

    [Flags]
    public enum DxfFaceModifier
    {
        None = 0,
        Opacity = 1,
        Specular = 2
    }

    public enum DxfEdgeStyleModel
    {
        NoEdges = 0,
        IsoLines = 1,
        FacetEdges = 2
    }

    public partial class DxfVisualStyle
    {
    }
}
