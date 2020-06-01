namespace IxMilia.Dxf.Objects
{
    public enum DxfSamplingFilterType
    {
        Box = 0,
        Triangle = 1,
        Gauss = 2,
        Mitchell = 3,
        Lanczos = 4
    }

    public enum DxfRenderShadowMode
    {
        Simple = 0,
        Sort = 1,
        Segment = 2
    }

    public enum DxfRenderDiagnosticMode
    {
        Off = 0,
        Grid = 1,
        Photon = 2,
        BSP = 4
    }

    public enum DxfRenderDiagnosticGridMode
    {
        Object = 0,
        World = 1,
        Camera = 2
    }

    public enum DxfDiagnosticPhotonMode
    {
        Density = 0,
        Irradiance = 1
    }

    public enum DxfDiagnosticBSPMode
    {
        Depth = 0,
        Size = 1
    }

    public enum DxfTileOrder
    {
        Hilbert = 0,
        Spiral = 1,
        LeftToRight = 2,
        RightToLeft = 3,
        TopToBottom = 4,
        BottomToTop = 5
    }

    public partial class DxfMentalRayRenderSettings
    {
    }
}
