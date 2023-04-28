using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace IxMilia.Dxf.Objects
{
    public class DxfRenderSettings
    {
        public int Version { get; set; } = 1;
        public string PresetName { get; set; }
        public bool RenderMaterials { get; set; }
        public int TextureSamplingQuality { get; set; }
        public bool RenderBackFaces { get; set; }
        public bool RenderShadows { get; set; }
        public string PreviewImageFileName { get; set; }
        public string PresetDescription { get; set; }
        public int DisplayIndex { get; set; }
        public bool IsPredefined { get; set; }

        // used to track the reader state
        private bool _startedReading = false;
        private bool _stoppedReading = false;
        private int _code_1_index = 0;
        private int _code_90_index = 0;
        private int _code_290_index = 0;

        internal void AddValuePairs(List<DxfCodePair> pairs)
        {
            ValidateVersion();
            pairs.Add(new DxfCodePair(100, "AcDbRenderSettings"));
            pairs.Add(new DxfCodePair(90, Version));
            pairs.Add(new DxfCodePair(1, PresetName));
            pairs.Add(new DxfCodePair(290, DxfCommonConverters.BoolShort(RenderMaterials)));
            switch (Version)
            {
                case 1:
                    pairs.Add(new DxfCodePair(90, TextureSamplingQuality));
                    break;
                case 2:
                    pairs.Add(new DxfCodePair(290, DxfCommonConverters.BoolShort(TextureSamplingQuality != 0)));
                    break;
            }

            pairs.Add(new DxfCodePair(290, DxfCommonConverters.BoolShort(RenderBackFaces)));
            pairs.Add(new DxfCodePair(290, DxfCommonConverters.BoolShort(RenderShadows)));
            pairs.Add(new DxfCodePair(1, PreviewImageFileName));
            if (Version == 2)
            {
                pairs.Add(new DxfCodePair(1, PresetDescription));
                pairs.Add(new DxfCodePair(90, DisplayIndex));
                pairs.Add(new DxfCodePair(290, DxfCommonConverters.BoolShort(IsPredefined)));
            }
        }

        internal bool TrySetPair(DxfCodePair pair)
        {
            if (_stoppedReading)
            {
                // we're done, don't check any more
                return false;
            }

            if (!_startedReading)
            {
                // haven't even started yet
                if (pair.Code == 100 &&
                    pair.StringValue == "AcDbRenderSettings")
                {
                    _startedReading = true;
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                // we've started, so start consuming
                switch (pair.Code)
                {
                    case 100:
                        // don't consume this, but don't read any more
                        _stoppedReading = true;
                        break;
                    case 1:
                        switch (_code_1_index)
                        {
                            case 0:
                                PresetName = pair.StringValue;
                                _code_1_index++;
                                return true;
                            case 1:
                                PreviewImageFileName = pair.StringValue;
                                _code_1_index++;
                                return true;
                            case 2:
                                if (Version == 2)
                                {
                                    PresetDescription = pair.StringValue;
                                    _code_1_index++;
                                    return true;
                                }
                                else
                                {
                                    goto default;
                                }
                            default:
                                Debug.Fail($"Unexpected code 1 index {_code_1_index} for version {Version}");
                                break;
                        }
                        break;
                    case 90:
                        switch (_code_90_index)
                        {
                            case 0:
                                Version = pair.IntegerValue;
                                ValidateVersion();
                                _code_90_index++;
                                return true;
                            case 1:
                                switch (Version)
                                {
                                    case 1:
                                        TextureSamplingQuality = pair.IntegerValue;
                                        _code_90_index++;
                                        return true;
                                    case 2:
                                        DisplayIndex = pair.IntegerValue;
                                        _code_90_index++;
                                        return true;
                                }
                                break;
                            default:
                                Debug.Fail($"Unexpected code 90 index {_code_90_index} for version {Version}");
                                break;
                        }
                        break;
                    case 290:
                        switch ((Version, _code_290_index))
                        {
                            case (1, 0):
                            case (2, 0):
                                RenderMaterials = pair.BoolValue;
                                _code_290_index++;
                                return true;
                            case (1, 1):
                            case (2, 2):
                                RenderBackFaces = pair.BoolValue;
                                _code_290_index++;
                                return true;
                            case (2, 1):
                                TextureSamplingQuality = pair.BoolValue ? 50 : 0;
                                _code_290_index++;
                                return true;
                            case (1, 2):
                            case (2, 3):
                                RenderShadows = pair.BoolValue;
                                _code_290_index++;
                                return true;
                            case (2, 4):
                                IsPredefined = pair.BoolValue;
                                _code_290_index++;
                                return true;
                            default:
                                Debug.Fail($"Unexpected code 290 index {_code_290_index} for version {Version}");
                                break;
                        }
                        break;
                }
            }

            return false;
        }

        private void ValidateVersion()
        {
            switch (Version)
            {
                case 1:
                case 2:
                    break;
                default:
                    throw new NotSupportedException($"{nameof(DxfRenderSettings)} version {Version} is not supported");
            }
        }
    }
}
