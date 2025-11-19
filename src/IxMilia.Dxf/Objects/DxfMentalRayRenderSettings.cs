using System.Collections.Generic;
using System.Diagnostics;

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
        public DxfRenderSettings RenderSettings { get; } = new DxfRenderSettings();

        protected override void AddValuePairs(List<DxfCodePair> pairs, DxfAcadVersion version, bool outputHandles, bool writeXData)
        {
            base.AddValuePairs(pairs, version, outputHandles, writeXData: false);
            RenderSettings.AddValuePairs(pairs);
            pairs.Add(new DxfCodePair(100, "AcDbMentalRayRenderSettings"));
            pairs.Add(new DxfCodePair(90, this.ClassVersion));
            pairs.Add(new DxfCodePair(90, this.MinimumSamplingRate));
            pairs.Add(new DxfCodePair(90, this.MaximumSamplingRate));
            pairs.Add(new DxfCodePair(70, (short)this.SamplingFilterType));
            pairs.Add(new DxfCodePair(40, this.FilterWidth));
            pairs.Add(new DxfCodePair(40, this.FilterHeight));
            pairs.Add(new DxfCodePair(40, this.SamplingContrastColor_Red));
            pairs.Add(new DxfCodePair(40, this.SamplingContrastColor_Green));
            pairs.Add(new DxfCodePair(40, this.SamplingContrastColor_Blue));
            pairs.Add(new DxfCodePair(40, this.SamplingContrastColor_Alpha));
            pairs.Add(new DxfCodePair(70, (short)this.ShadowMode));
            pairs.Add(new DxfCodePair(290, this.MapShadows));
            pairs.Add(new DxfCodePair(290, this.RayTracing));
            pairs.Add(new DxfCodePair(90, this.RayTracingDepth_Reflections));
            pairs.Add(new DxfCodePair(90, this.RayTracingDepth_Refractions));
            pairs.Add(new DxfCodePair(90, this.RayTracingDepth_Maximum));
            pairs.Add(new DxfCodePair(290, this.UseGlobalIllumination));
            pairs.Add(new DxfCodePair(90, this.SampleCount));
            pairs.Add(new DxfCodePair(290, this.UseGlobalIlluminationRadius));
            pairs.Add(new DxfCodePair(40, this.GlobalIlluminationRadius));
            pairs.Add(new DxfCodePair(90, this.PhotonsPerLight));
            pairs.Add(new DxfCodePair(90, this.GlobalIlluminationDepth_Reflections));
            pairs.Add(new DxfCodePair(90, this.GlobalIlluminationDepth_Refractions));
            pairs.Add(new DxfCodePair(90, this.GlobalIlluminationDepth_Maximum));
            pairs.Add(new DxfCodePair(290, this.UseFinalGather));
            pairs.Add(new DxfCodePair(90, this.FinalGatherRayCount));
            pairs.Add(new DxfCodePair(290, this.UseFinalGatherMinimumRadius));
            pairs.Add(new DxfCodePair(290, this.UseFinalGatherMaximumRadius));
            pairs.Add(new DxfCodePair(290, this.UseFinalGatherPixels));
            pairs.Add(new DxfCodePair(40, this.FinalGatherSampleRadius_Minimum));
            pairs.Add(new DxfCodePair(40, this.FinalGatherSampleRadius_Maximum));
            pairs.Add(new DxfCodePair(40, this.LuminanceScale));
            pairs.Add(new DxfCodePair(70, (short)this.DiagnosticMode));
            pairs.Add(new DxfCodePair(70, (short)this.DiagnosticGridMode));
            pairs.Add(new DxfCodePair(40, this.GridSize));
            pairs.Add(new DxfCodePair(70, (short)this.DiagnosticPhotonMode));
            pairs.Add(new DxfCodePair(70, (short)this.DiagnosticBSPMode));
            pairs.Add(new DxfCodePair(290, this.ExportMIStatistics));
            pairs.Add(new DxfCodePair(1, this.MIStatisticsFileName));
            pairs.Add(new DxfCodePair(90, this.TileSize));
            pairs.Add(new DxfCodePair(70, (short)this.TileOrder));
            pairs.Add(new DxfCodePair(90, this.MemoryLimit));
            if (writeXData)
            {
                DxfXData.AddValuePairs(XData, pairs, version, outputHandles);
            }
        }

        // This object has vales that share codes between properties and these counters are used to know which property to
        // assign to in TrySetPair() below.
        private int _code_40_index = 0; // shared by properties FilterWidth, FilterHeight, SamplingContrastColor_Red, SamplingContrastColor_Green, SamplingContrastColor_Blue, SamplingContrastColor_Alpha, GlobalIlluminationRadius, FinalGatherSampleRadius_Minimum, FinalGatherSampleRadius_Maximum, LuminanceScale, GridSize
        private int _code_70_index = 0; // shared by properties SamplingFilterType, ShadowMode, DiagnosticMode, DiagnosticGridMode, DiagnosticPhotonMode, DiagnosticBSPMode, TileOrder
        private int _code_90_index = 0; // shared by properties ClassVersion, MinimumSamplingRate, MaximumSamplingRate, RayTracingDepth_Reflections, RayTracingDepth_Refractions, RayTracingDepth_Maximum, SampleCount, PhotonsPerLight, GlobalIlluminationDepth_Reflections, GlobalIlluminationDepth_Refractions, GlobalIlluminationDepth_Maximum, FinalGatherRayCount, TileSize, MemoryLimit
        private int _code_290_index = 0; // shared by properties MapShadows, RayTracing, UseGlobalIllumination, UseGlobalIlluminationRadius, UseFinalGather, UseFinalGatherMinimumRadius, UseFinalGatherMaximumRadius, UseFinalGatherPixels, ExportMIStatistics

        internal override bool TrySetPair(DxfCodePair pair)
        {
            // the settings object gets the first crack at reading code pairs
            if (RenderSettings.TrySetPair(pair))
            {
                return true;
            }

            switch (pair.Code)
            {
                case 1:
                    this.MIStatisticsFileName = pair.StringValue;
                    break;
                case 40:
                    switch (_code_40_index)
                    {
                        case 0:
                            this.FilterWidth = pair.DoubleValue;
                            _code_40_index++;
                            break;
                        case 1:
                            this.FilterHeight = pair.DoubleValue;
                            _code_40_index++;
                            break;
                        case 2:
                            this.SamplingContrastColor_Red = pair.DoubleValue;
                            _code_40_index++;
                            break;
                        case 3:
                            this.SamplingContrastColor_Green = pair.DoubleValue;
                            _code_40_index++;
                            break;
                        case 4:
                            this.SamplingContrastColor_Blue = pair.DoubleValue;
                            _code_40_index++;
                            break;
                        case 5:
                            this.SamplingContrastColor_Alpha = pair.DoubleValue;
                            _code_40_index++;
                            break;
                        case 6:
                            this.GlobalIlluminationRadius = pair.DoubleValue;
                            _code_40_index++;
                            break;
                        case 7:
                            this.FinalGatherSampleRadius_Minimum = pair.DoubleValue;
                            _code_40_index++;
                            break;
                        case 8:
                            this.FinalGatherSampleRadius_Maximum = pair.DoubleValue;
                            _code_40_index++;
                            break;
                        case 9:
                            this.LuminanceScale = pair.DoubleValue;
                            _code_40_index++;
                            break;
                        case 10:
                            this.GridSize = pair.DoubleValue;
                            _code_40_index++;
                            break;
                        case 11:
                            this._unknown_code_40 = pair.DoubleValue;
                            _code_40_index++;
                            break;
                        default:
                            Debug.Assert(false, "Unexpected extra values for code 40");
                            break;
                    }
                    break;
                case 70:
                    switch (_code_70_index)
                    {
                        case 0:
                            this.SamplingFilterType = (DxfSamplingFilterType)pair.ShortValue;
                            _code_70_index++;
                            break;
                        case 1:
                            this.ShadowMode = (DxfRenderShadowMode)pair.ShortValue;
                            _code_70_index++;
                            break;
                        case 2:
                            this.DiagnosticMode = (DxfRenderDiagnosticMode)pair.ShortValue;
                            _code_70_index++;
                            break;
                        case 3:
                            this.DiagnosticGridMode = (DxfRenderDiagnosticGridMode)pair.ShortValue;
                            _code_70_index++;
                            break;
                        case 4:
                            this.DiagnosticPhotonMode = (DxfDiagnosticPhotonMode)pair.ShortValue;
                            _code_70_index++;
                            break;
                        case 5:
                            this.DiagnosticBSPMode = (DxfDiagnosticBSPMode)pair.ShortValue;
                            _code_70_index++;
                            break;
                        case 6:
                            this.TileOrder = (DxfTileOrder)pair.ShortValue;
                            _code_70_index++;
                            break;
                        default:
                            Debug.Assert(false, "Unexpected extra values for code 70");
                            break;
                    }
                    break;
                case 90:
                    switch (_code_90_index)
                    {
                        case 0:
                            this.ClassVersion = pair.IntegerValue;
                            _code_90_index++;
                            break;
                        case 1:
                            this.MinimumSamplingRate = pair.IntegerValue;
                            _code_90_index++;
                            break;
                        case 2:
                            this.MaximumSamplingRate = pair.IntegerValue;
                            _code_90_index++;
                            break;
                        case 3:
                            this.RayTracingDepth_Reflections = pair.IntegerValue;
                            _code_90_index++;
                            break;
                        case 4:
                            this.RayTracingDepth_Refractions = pair.IntegerValue;
                            _code_90_index++;
                            break;
                        case 5:
                            this.RayTracingDepth_Maximum = pair.IntegerValue;
                            _code_90_index++;
                            break;
                        case 6:
                            this.SampleCount = pair.IntegerValue;
                            _code_90_index++;
                            break;
                        case 7:
                            this.PhotonsPerLight = pair.IntegerValue;
                            _code_90_index++;
                            break;
                        case 8:
                            this.GlobalIlluminationDepth_Reflections = pair.IntegerValue;
                            _code_90_index++;
                            break;
                        case 9:
                            this.GlobalIlluminationDepth_Refractions = pair.IntegerValue;
                            _code_90_index++;
                            break;
                        case 10:
                            this.GlobalIlluminationDepth_Maximum = pair.IntegerValue;
                            _code_90_index++;
                            break;
                        case 11:
                            this.FinalGatherRayCount = pair.IntegerValue;
                            _code_90_index++;
                            break;
                        case 12:
                            this.TileSize = pair.IntegerValue;
                            _code_90_index++;
                            break;
                        case 13:
                            this.MemoryLimit = pair.IntegerValue;
                            _code_90_index++;
                            break;
                        default:
                            Debug.Assert(false, "Unexpected extra values for code 90");
                            break;
                    }
                    break;
                case 290:
                    switch (_code_290_index)
                    {
                        case 0:
                            this.MapShadows = pair.BoolValue;
                            _code_290_index++;
                            break;
                        case 1:
                            this.RayTracing = pair.BoolValue;
                            _code_290_index++;
                            break;
                        case 2:
                            this.UseGlobalIllumination = pair.BoolValue;
                            _code_290_index++;
                            break;
                        case 3:
                            this.UseGlobalIlluminationRadius = pair.BoolValue;
                            _code_290_index++;
                            break;
                        case 4:
                            this.UseFinalGather = pair.BoolValue;
                            _code_290_index++;
                            break;
                        case 5:
                            this.UseFinalGatherMinimumRadius = pair.BoolValue;
                            _code_290_index++;
                            break;
                        case 6:
                            this.UseFinalGatherMaximumRadius = pair.BoolValue;
                            _code_290_index++;
                            break;
                        case 7:
                            this.UseFinalGatherPixels = pair.BoolValue;
                            _code_290_index++;
                            break;
                        case 8:
                            this.ExportMIStatistics = pair.BoolValue;
                            _code_290_index++;
                            break;
                        case 9:
                            this._unknown_code_290 = pair.BoolValue;
                            _code_290_index++;
                            break;
                        default:
                            Debug.Assert(false, "Unexpected extra values for code 290");
                            break;
                    }
                    break;
                default:
                    return base.TrySetPair(pair);
            }

            return true;
        }
    }
}
