// Copyright (c) IxMilia.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;
using System.Linq;

namespace IxMilia.Dxf.Objects
{
    public enum DxfMapProjectionMethod
    {
        Planar = 1,
        Box = 2,
        Cylinder = 3,
        Sphere = 4
    }

    public enum DxfMapTilingMethod
    {
        Tile = 1,
        Crop = 2,
        Clamp = 3
    }

    public enum DxfMapAutoTransformMethod
    {
        NoAutoTransform = 1,
        ScaleToCurrentEntity = 2,
        IncludeCurrentBlockTransform = 4
    }

    public partial class DxfMaterial
    {
        public DxfTransformationMatrix DiffuseMapTransformMatrix { get; set; } = DxfTransformationMatrix.Identity;
        public DxfTransformationMatrix SpecularMapTransformMatrix { get; set; } = DxfTransformationMatrix.Identity;
        public DxfTransformationMatrix ReflectionMapTransformMatrix { get; set; } = DxfTransformationMatrix.Identity;
        public DxfTransformationMatrix OpacityMapTransformMatrix { get; set; } = DxfTransformationMatrix.Identity;
        public DxfTransformationMatrix BumpMapTransformMatrix { get; set; } = DxfTransformationMatrix.Identity;
        public DxfTransformationMatrix RefractionMapTransformMatrix { get; set; } = DxfTransformationMatrix.Identity;
        public DxfTransformationMatrix NormalMapTransformMatrix { get; set; } = DxfTransformationMatrix.Identity;

        // This object has vales that share codes between properties and these counters are used to know which property to
        // assign to in the switch below.
        private int _code_3_index = 0; // shared by properties DiffuseMapFileName, NormalMapFileName
        private int _code_42_index = 0; // shared by properties DiffuseMapBlendFactor, NormalMapBlendFactor
        private int _code_72_index = 0; // shared by properties UseImageFileForDiffuseMap, UseImageFileForNormalMap
        private int _code_73_index = 0; // shared by properties DiffuseMapProjectionMethod, NormalMapProjectionMethod
        private int _code_74_index = 0; // shared by properties DiffuseMapTilingMethod, NormalMapTilingMethod
        private int _code_75_index = 0; // shared by properties DiffuseMapAutoTransformMethod, NormalMapAutoTransformMethod
        private int _code_90_index = 0; // shared by properties AmbientColorValue, SelfIllumination
        private int _code_270_index = 0; // shared by properties BumpMapProjectionMethod, LuminanceMode, MapUTile
        private int _code_271_index = 0; // shared by properties BumpMapTilingMethod, NormalMapMethod, GenProcIntegerValue
        private int _code_272_index = 0; // shared by properties BumpMapAutoTransformMethod, GlobalIlluminationMode
        private int _code_273_index = 0; // shared by properties UseImageFileForRefractionMap, FinalGatherMode

        internal override DxfObject PopulateFromBuffer(DxfCodePairBufferReader buffer)
        {
            bool isReadingNormal = false;
            while (buffer.ItemsRemain)
            {
                var pair = buffer.Peek();
                if (pair.Code == 0)
                {
                    break;
                }

                while (this.TrySetExtensionData(pair, buffer))
                {
                    pair = buffer.Peek();
                }

                switch (pair.Code)
                {
                    case 1:
                        this.Name = (pair.StringValue);
                        break;
                    case 2:
                        this.Description = (pair.StringValue);
                        break;
                    case 3:
                        switch (_code_3_index)
                        {
                            case 0:
                                this.DiffuseMapFileName = (pair.StringValue);
                                _code_3_index++;
                                break;
                            case 1:
                                this.NormalMapFileName = (pair.StringValue);
                                isReadingNormal = true;
                                _code_3_index++;
                                break;
                            default:
                                Debug.Assert(false, "Unexpected extra values for code 3");
                                break;
                        }
                        break;
                    case 4:
                        this.SpecularMapFileName = (pair.StringValue);
                        break;
                    case 6:
                        this.ReflectionMapFileName = (pair.StringValue);
                        break;
                    case 7:
                        this.OpacityMapFileName = (pair.StringValue);
                        break;
                    case 8:
                        this.BumpMapFileName = (pair.StringValue);
                        break;
                    case 9:
                        this.RefractionMapFileName = (pair.StringValue);
                        break;
                    case 40:
                        this.AmbientColorFactor = (pair.DoubleValue);
                        break;
                    case 41:
                        this.DiffuseColorFactor = (pair.DoubleValue);
                        break;
                    case 42:
                        switch (_code_42_index)
                        {
                            case 0:
                                this.DiffuseMapBlendFactor = (pair.DoubleValue);
                                _code_42_index++;
                                break;
                            case 1:
                                this.NormalMapBlendFactor = (pair.DoubleValue);
                                isReadingNormal = true;
                                _code_42_index++;
                                break;
                            default:
                                Debug.Assert(false, "Unexpected extra values for code 42");
                                break;
                        }
                        break;
                    case 43:
                        if (isReadingNormal)
                        {
                            this._normalMapTransformMatrixValues.Add(pair.DoubleValue);
                        }
                        else
                        {
                            this._diffuseMapTransformMatrixValues.Add(pair.DoubleValue);
                        }
                        break;
                    case 44:
                        this.SpecularGlossFactor = (pair.DoubleValue);
                        break;
                    case 45:
                        this.SpecularColorFactor = (pair.DoubleValue);
                        break;
                    case 46:
                        this.SpecularMapBlendFactor = (pair.DoubleValue);
                        break;
                    case 47:
                        this._specularMapTransformMatrixValues.Add((pair.DoubleValue));
                        break;
                    case 48:
                        this.ReflectionMapBlendFactor = (pair.DoubleValue);
                        break;
                    case 49:
                        this._reflectionMapTransformMatrixValues.Add((pair.DoubleValue));
                        break;
                    case 62:
                        this.GenProcColorIndexValue = DxfColor.FromRawValue(pair.ShortValue);
                        break;
                    case 70:
                        this.OverrideAmbientColor = BoolShort(pair.ShortValue);
                        break;
                    case 71:
                        this.OverrideDiffuseColor = BoolShort(pair.ShortValue);
                        break;
                    case 72:
                        switch (_code_72_index)
                        {
                            case 0:
                                this.UseImageFileForDiffuseMap = BoolShort(pair.ShortValue);
                                _code_72_index++;
                                break;
                            case 1:
                                this.UseImageFileForNormalMap = BoolShort(pair.ShortValue);
                                _code_72_index++;
                                break;
                            default:
                                Debug.Assert(false, "Unexpected extra values for code 72");
                                break;
                        }
                        break;
                    case 73:
                        switch (_code_73_index)
                        {
                            case 0:
                                this.DiffuseMapProjectionMethod = (DxfMapProjectionMethod)(pair.ShortValue);
                                _code_73_index++;
                                break;
                            case 1:
                                this.NormalMapProjectionMethod = (DxfMapProjectionMethod)(pair.ShortValue);
                                isReadingNormal = true;
                                _code_73_index++;
                                break;
                            default:
                                Debug.Assert(false, "Unexpected extra values for code 73");
                                break;
                        }
                        break;
                    case 74:
                        switch (_code_74_index)
                        {
                            case 0:
                                this.DiffuseMapTilingMethod = (DxfMapTilingMethod)(pair.ShortValue);
                                _code_74_index++;
                                break;
                            case 1:
                                this.NormalMapTilingMethod = (DxfMapTilingMethod)(pair.ShortValue);
                                isReadingNormal = true;
                                _code_74_index++;
                                break;
                            default:
                                Debug.Assert(false, "Unexpected extra values for code 74");
                                break;
                        }
                        break;
                    case 75:
                        switch (_code_75_index)
                        {
                            case 0:
                                this.DiffuseMapAutoTransformMethod = (DxfMapAutoTransformMethod)(pair.ShortValue);
                                _code_75_index++;
                                break;
                            case 1:
                                this.NormalMapAutoTransformMethod = (DxfMapAutoTransformMethod)(pair.ShortValue);
                                isReadingNormal = true;
                                _code_75_index++;
                                break;
                            default:
                                Debug.Assert(false, "Unexpected extra values for code 75");
                                break;
                        }
                        break;
                    case 76:
                        this.OverrideSpecularColor = BoolShort(pair.ShortValue);
                        break;
                    case 77:
                        this.UseImageFileForSpecularMap = BoolShort(pair.ShortValue);
                        break;
                    case 78:
                        this.SpecularMapProjectionMethod = (DxfMapProjectionMethod)(pair.ShortValue);
                        break;
                    case 79:
                        this.SpecularMapTilingMethod = (DxfMapTilingMethod)(pair.ShortValue);
                        break;
                    case 90:
                        switch (_code_90_index)
                        {
                            case 0:
                                this.AmbientColorValue = (pair.IntegerValue);
                                _code_90_index++;
                                break;
                            case 1:
                                this.SelfIllumination = (pair.IntegerValue);
                                _code_90_index++;
                                break;
                            default:
                                Debug.Assert(false, "Unexpected extra values for code 90");
                                break;
                        }
                        break;
                    case 91:
                        this.DiffuseColorValue = (pair.IntegerValue);
                        break;
                    case 92:
                        this.SpecularColorValue = (pair.IntegerValue);
                        break;
                    case 93:
                        this.IlluminationModel = (pair.IntegerValue);
                        break;
                    case 94:
                        this.ChannelFlags = (pair.IntegerValue);
                        break;
                    case 140:
                        this.OpacityFactor = (pair.DoubleValue);
                        break;
                    case 141:
                        this.OpacityMapBlendFactor = (pair.DoubleValue);
                        break;
                    case 142:
                        this._opacityMapTransformMatrixValues.Add((pair.DoubleValue));
                        break;
                    case 143:
                        this.BumpMapBlendFactor = (pair.DoubleValue);
                        break;
                    case 144:
                        this._bumpMapTransformMatrixValues.Add((pair.DoubleValue));
                        break;
                    case 145:
                        this.RefractionIndex = (pair.DoubleValue);
                        break;
                    case 146:
                        this.RefractionMapBlendFactor = (pair.DoubleValue);
                        break;
                    case 147:
                        this._refractionMapTransformMatrixValues.Add((pair.DoubleValue));
                        break;
                    case 148:
                        this.Translucence = (pair.DoubleValue);
                        break;
                    case 170:
                        this.SpecularMapAutoTransformMethod = (DxfMapAutoTransformMethod)(pair.ShortValue);
                        break;
                    case 171:
                        this.UseImageFileForReflectionMap = BoolShort(pair.ShortValue);
                        break;
                    case 172:
                        this.ReflectionMapProjectionMethod = (DxfMapProjectionMethod)(pair.ShortValue);
                        break;
                    case 173:
                        this.ReflectionMapTilingMethod = (DxfMapTilingMethod)(pair.ShortValue);
                        break;
                    case 174:
                        this.ReflectionMapAutoTransformMethod = (DxfMapAutoTransformMethod)(pair.ShortValue);
                        break;
                    case 175:
                        this.UseImageFileForOpacityMap = BoolShort(pair.ShortValue);
                        break;
                    case 176:
                        this.OpacityMapProjectionMethod = (DxfMapProjectionMethod)(pair.ShortValue);
                        break;
                    case 177:
                        this.OpacityMapTilingMethod = (DxfMapTilingMethod)(pair.ShortValue);
                        break;
                    case 178:
                        this.OpacityMapAutoTransformMethod = (DxfMapAutoTransformMethod)(pair.ShortValue);
                        break;
                    case 179:
                        this.UseImageFileForBumpMap = BoolShort(pair.ShortValue);
                        break;
                    case 270:
                        switch (_code_270_index)
                        {
                            case 0:
                                this.BumpMapProjectionMethod = (DxfMapProjectionMethod)(pair.ShortValue);
                                _code_270_index++;
                                break;
                            case 1:
                                this.LuminanceMode = (pair.ShortValue);
                                _code_270_index++;
                                break;
                            case 2:
                                this.MapUTile = (pair.ShortValue);
                                _code_270_index++;
                                break;
                            default:
                                Debug.Assert(false, "Unexpected extra values for code 270");
                                break;
                        }
                        break;
                    case 271:
                        switch (_code_271_index)
                        {
                            case 0:
                                this.BumpMapTilingMethod = (DxfMapTilingMethod)(pair.ShortValue);
                                _code_271_index++;
                                break;
                            case 1:
                                this.NormalMapMethod = (pair.ShortValue);
                                _code_271_index++;
                                break;
                            case 2:
                                this.GenProcIntegerValue = (pair.ShortValue);
                                _code_271_index++;
                                break;
                            default:
                                Debug.Assert(false, "Unexpected extra values for code 271");
                                break;
                        }
                        break;
                    case 272:
                        switch (_code_272_index)
                        {
                            case 0:
                                this.BumpMapAutoTransformMethod = (DxfMapAutoTransformMethod)(pair.ShortValue);
                                _code_272_index++;
                                break;
                            case 1:
                                this.GlobalIlluminationMode = (pair.ShortValue);
                                _code_272_index++;
                                break;
                            default:
                                Debug.Assert(false, "Unexpected extra values for code 272");
                                break;
                        }
                        break;
                    case 273:
                        switch (_code_273_index)
                        {
                            case 0:
                                this.UseImageFileForRefractionMap = BoolShort(pair.ShortValue);
                                _code_273_index++;
                                break;
                            case 1:
                                this.FinalGatherMode = (pair.ShortValue);
                                _code_273_index++;
                                break;
                            default:
                                Debug.Assert(false, "Unexpected extra values for code 273");
                                break;
                        }
                        break;
                    case 274:
                        this.RefractionMapProjectionMethod = (DxfMapProjectionMethod)(pair.ShortValue);
                        break;
                    case 275:
                        this.RefractionMapTilingMethod = (DxfMapTilingMethod)(pair.ShortValue);
                        break;
                    case 276:
                        this.RefractionMapAutoTransformMethod = (DxfMapAutoTransformMethod)(pair.ShortValue);
                        break;
                    case 290:
                        this.IsTwoSided = (pair.BoolValue);
                        break;
                    case 291:
                        this.GenProcBooleanValue = (pair.BoolValue);
                        break;
                    case 292:
                        this.GenProcTableEnd = (pair.BoolValue);
                        break;
                    case 293:
                        this.IsAnonymous = (pair.BoolValue);
                        break;
                    case 300:
                        this.GenProcName = (pair.StringValue);
                        break;
                    case 301:
                        this.GenProcTextValue = (pair.StringValue);
                        break;
                    case 420:
                        this.GenProcColorRGBValue = (pair.IntegerValue);
                        break;
                    case 430:
                        this.GenProcColorName = (pair.StringValue);
                        break;
                    case 460:
                        this.ColorBleedScale = (pair.DoubleValue);
                        break;
                    case 461:
                        this.IndirectDumpScale = (pair.DoubleValue);
                        break;
                    case 462:
                        this.ReflectanceScale = (pair.DoubleValue);
                        break;
                    case 463:
                        this.TransmittanceScale = (pair.DoubleValue);
                        break;
                    case 464:
                        this.Luminance = (pair.DoubleValue);
                        break;
                    case 465:
                        this.NormalMapStrength = (pair.DoubleValue);
                        isReadingNormal = true;
                        break;
                    case 468:
                        this.Reflectivity = (pair.DoubleValue);
                        break;
                    case 469:
                        this.GenProcRealValue = (pair.DoubleValue);
                        break;
                    default:
                        base.TrySetPair(pair);
                        break;
                }

                buffer.Advance();
            }

            return PostParse();
        }

        protected override DxfObject PostParse()
        {
            DiffuseMapTransformMatrix = new DxfTransformationMatrix(_diffuseMapTransformMatrixValues.ToArray());
            SpecularMapTransformMatrix = new DxfTransformationMatrix(_specularMapTransformMatrixValues.ToArray());
            ReflectionMapTransformMatrix = new DxfTransformationMatrix(_reflectionMapTransformMatrixValues.ToArray());
            OpacityMapTransformMatrix = new DxfTransformationMatrix(_opacityMapTransformMatrixValues.ToArray());
            BumpMapTransformMatrix = new DxfTransformationMatrix(_bumpMapTransformMatrixValues.ToArray());
            RefractionMapTransformMatrix = new DxfTransformationMatrix(_refractionMapTransformMatrixValues.ToArray());
            NormalMapTransformMatrix = new DxfTransformationMatrix(_normalMapTransformMatrixValues.ToArray());
            _diffuseMapTransformMatrixValues.Clear();
            _specularMapTransformMatrixValues.Clear();
            _reflectionMapTransformMatrixValues.Clear();
            _opacityMapTransformMatrixValues.Clear();
            _bumpMapTransformMatrixValues.Clear();
            _refractionMapTransformMatrixValues.Clear();
            _normalMapTransformMatrixValues.Clear();
            return this;
        }
    }
}
