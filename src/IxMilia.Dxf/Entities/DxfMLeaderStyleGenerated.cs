// Copyright (c) IxMilia.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

// The contents of this file are automatically generated by a tool, and should not be directly modified.

using System;
using System.Collections.Generic;
using System.Linq;
using IxMilia.Dxf.Collections;

namespace IxMilia.Dxf.Entities
{

    /// <summary>
    /// DxfMLeaderStyle class
    /// </summary>
    public partial class DxfMLeaderStyle : DxfEntity, IDxfItemInternal
    {
        public override DxfEntityType EntityType { get { return DxfEntityType.MLeaderStyle; } }
        protected override DxfAcadVersion MinVersion { get { return DxfAcadVersion.R2007; } }


        IEnumerable<DxfPointer> IDxfItemInternal.GetPointers()
        {
            yield return LineLeaderTypePointer;
            yield return ArrowheadPointer;
            yield return MTextStylePointer;
            yield return BlockContentPointer;
        }

        IEnumerable<IDxfItemInternal> IDxfItemInternal.GetChildItems()
        {
            return ((IDxfItemInternal)this).GetPointers().Select(p => (IDxfItemInternal)p.Item);
        }

        internal DxfPointer LineLeaderTypePointer { get; } = new DxfPointer();
        internal DxfPointer ArrowheadPointer { get; } = new DxfPointer();
        internal DxfPointer MTextStylePointer { get; } = new DxfPointer();
        internal DxfPointer BlockContentPointer { get; } = new DxfPointer();

        public short ContentType { get; set; }
        public short DrawMLeaderOrderType { get; set; }
        public short DrawLeaderOrderType { get; set; }
        public int MaxLeaderSegmentCount { get; set; }
        public double FirstSegmentAngleConstraint { get; set; }
        public double SecondSegmentAngleConstraint { get; set; }
        public short LeaderLineType { get; set; }
        public int LeaderLineColor { get; set; }
        public IDxfItem LineLeaderType { get { return LineLeaderTypePointer.Item as IDxfItem; } set { LineLeaderTypePointer.Item = value; } }
        public int LeaderLineWeight { get; set; }
        public bool EnableLanding { get; set; }
        public double LandingGap { get; set; }
        public bool EnableDogleg { get; set; }
        public double DoglegLength { get; set; }
        public string MLeaderStyleDescription { get; set; }
        public IDxfItem Arrowhead { get { return ArrowheadPointer.Item as IDxfItem; } set { ArrowheadPointer.Item = value; } }
        public double ArrowheadSize { get; set; }
        public string DefaultMTextContents { get; set; }
        public IDxfItem MTextStyle { get { return MTextStylePointer.Item as IDxfItem; } set { MTextStylePointer.Item = value; } }
        public short TextLeftAttachmentType { get; set; }
        public short TextAngleType { get; set; }
        public short TextAlignmentType { get; set; }
        public short TextRightAttachmentType { get; set; }
        public int TextColor { get; set; }
        public double TextHeight { get; set; }
        public bool EnableFrameText { get; set; }
        public bool AlwaysAlignTextLeft { get; set; }
        public double AlignGap { get; set; }
        public IDxfItem BlockContent { get { return BlockContentPointer.Item as IDxfItem; } set { BlockContentPointer.Item = value; } }
        public int BlockContentColor { get; set; }
        public double BlockContentXScale { get; set; }
        public double BlockContentYScale { get; set; }
        public double BlockContentZScale { get; set; }
        public bool EnableBlockContentScale { get; set; }
        public double BlockContentRotation { get; set; }
        public bool EnableBlockContentRotation { get; set; }
        public short BlockContentConnectionType { get; set; }
        public double Scale { get; set; }
        public bool OverwritePropertyValue { get; set; }
        public bool IsAnnotative { get; set; }
        public double BreakGapSize { get; set; }
        public DxfTextAttachmentDirection TextAttachmentDirection { get; set; }
        public DxfBottomTextAttachmentDirection BottomTextAttachmentDirection { get; set; }
        public DxfTopTextAttachmentDirection TopTextAttachmentDirection { get; set; }

        public DxfMLeaderStyle()
            : base()
        {
        }

        protected override void Initialize()
        {
            base.Initialize();
            this.ContentType = 0;
            this.DrawMLeaderOrderType = 0;
            this.DrawLeaderOrderType = 0;
            this.MaxLeaderSegmentCount = 0;
            this.FirstSegmentAngleConstraint = 0.0;
            this.SecondSegmentAngleConstraint = 0.0;
            this.LeaderLineType = 0;
            this.LeaderLineColor = 0;
            this.LeaderLineWeight = 0;
            this.EnableLanding = true;
            this.LandingGap = 0.0;
            this.EnableDogleg = true;
            this.DoglegLength = 0.0;
            this.MLeaderStyleDescription = null;
            this.ArrowheadSize = 0.0;
            this.DefaultMTextContents = null;
            this.TextLeftAttachmentType = 0;
            this.TextAngleType = 0;
            this.TextAlignmentType = 0;
            this.TextRightAttachmentType = 0;
            this.TextColor = 0;
            this.TextHeight = 0.0;
            this.EnableFrameText = true;
            this.AlwaysAlignTextLeft = true;
            this.AlignGap = 0.0;
            this.BlockContentColor = 0;
            this.BlockContentXScale = 1.0;
            this.BlockContentYScale = 1.0;
            this.BlockContentZScale = 1.0;
            this.EnableBlockContentScale = true;
            this.BlockContentRotation = 0.0;
            this.EnableBlockContentRotation = true;
            this.BlockContentConnectionType = 0;
            this.Scale = 1.0;
            this.OverwritePropertyValue = false;
            this.IsAnnotative = true;
            this.BreakGapSize = 0.0;
            this.TextAttachmentDirection = DxfTextAttachmentDirection.Horizontal;
            this.BottomTextAttachmentDirection = DxfBottomTextAttachmentDirection.Center;
            this.TopTextAttachmentDirection = DxfTopTextAttachmentDirection.Center;
        }

        protected override void AddValuePairs(List<DxfCodePair> pairs, DxfAcadVersion version, bool outputHandles)
        {
            base.AddValuePairs(pairs, version, outputHandles);
            pairs.Add(new DxfCodePair(170, (this.ContentType)));
            pairs.Add(new DxfCodePair(171, (this.DrawMLeaderOrderType)));
            pairs.Add(new DxfCodePair(172, (this.DrawLeaderOrderType)));
            pairs.Add(new DxfCodePair(90, (this.MaxLeaderSegmentCount)));
            pairs.Add(new DxfCodePair(40, (this.FirstSegmentAngleConstraint)));
            pairs.Add(new DxfCodePair(41, (this.SecondSegmentAngleConstraint)));
            pairs.Add(new DxfCodePair(173, (this.LeaderLineType)));
            pairs.Add(new DxfCodePair(91, (this.LeaderLineColor)));
            pairs.Add(new DxfCodePair(340, DxfCommonConverters.UIntHandle(this.LineLeaderTypePointer.Handle)));
            pairs.Add(new DxfCodePair(92, (this.LeaderLineWeight)));
            pairs.Add(new DxfCodePair(290, (this.EnableLanding)));
            pairs.Add(new DxfCodePair(42, (this.LandingGap)));
            pairs.Add(new DxfCodePair(291, (this.EnableDogleg)));
            pairs.Add(new DxfCodePair(43, (this.DoglegLength)));
            pairs.Add(new DxfCodePair(3, (this.MLeaderStyleDescription)));
            pairs.Add(new DxfCodePair(341, DxfCommonConverters.UIntHandle(this.ArrowheadPointer.Handle)));
            pairs.Add(new DxfCodePair(44, (this.ArrowheadSize)));
            pairs.Add(new DxfCodePair(300, (this.DefaultMTextContents)));
            pairs.Add(new DxfCodePair(342, DxfCommonConverters.UIntHandle(this.MTextStylePointer.Handle)));
            pairs.Add(new DxfCodePair(174, (this.TextLeftAttachmentType)));
            pairs.Add(new DxfCodePair(175, (this.TextAngleType)));
            pairs.Add(new DxfCodePair(176, (this.TextAlignmentType)));
            pairs.Add(new DxfCodePair(178, (this.TextRightAttachmentType)));
            pairs.Add(new DxfCodePair(93, (this.TextColor)));
            pairs.Add(new DxfCodePair(45, (this.TextHeight)));
            pairs.Add(new DxfCodePair(292, (this.EnableFrameText)));
            pairs.Add(new DxfCodePair(297, (this.AlwaysAlignTextLeft)));
            pairs.Add(new DxfCodePair(46, (this.AlignGap)));
            pairs.Add(new DxfCodePair(343, DxfCommonConverters.UIntHandle(this.BlockContentPointer.Handle)));
            pairs.Add(new DxfCodePair(94, (this.BlockContentColor)));
            pairs.Add(new DxfCodePair(47, (this.BlockContentXScale)));
            pairs.Add(new DxfCodePair(49, (this.BlockContentYScale)));
            pairs.Add(new DxfCodePair(140, (this.BlockContentZScale)));
            pairs.Add(new DxfCodePair(293, (this.EnableBlockContentScale)));
            pairs.Add(new DxfCodePair(141, (this.BlockContentRotation)));
            pairs.Add(new DxfCodePair(294, (this.EnableBlockContentRotation)));
            pairs.Add(new DxfCodePair(177, (this.BlockContentConnectionType)));
            pairs.Add(new DxfCodePair(142, (this.Scale)));
            pairs.Add(new DxfCodePair(295, (this.OverwritePropertyValue)));
            pairs.Add(new DxfCodePair(296, (this.IsAnnotative)));
            pairs.Add(new DxfCodePair(143, (this.BreakGapSize)));
            pairs.Add(new DxfCodePair(271, (short)(this.TextAttachmentDirection)));
            pairs.Add(new DxfCodePair(272, (short)(this.BottomTextAttachmentDirection)));
            pairs.Add(new DxfCodePair(273, (short)(this.TopTextAttachmentDirection)));
        }

        internal override bool TrySetPair(DxfCodePair pair)
        {
            switch (pair.Code)
            {
                case 3:
                    this.MLeaderStyleDescription = (pair.StringValue);
                    break;
                case 40:
                    this.FirstSegmentAngleConstraint = (pair.DoubleValue);
                    break;
                case 41:
                    this.SecondSegmentAngleConstraint = (pair.DoubleValue);
                    break;
                case 42:
                    this.LandingGap = (pair.DoubleValue);
                    break;
                case 43:
                    this.DoglegLength = (pair.DoubleValue);
                    break;
                case 44:
                    this.ArrowheadSize = (pair.DoubleValue);
                    break;
                case 45:
                    this.TextHeight = (pair.DoubleValue);
                    break;
                case 46:
                    this.AlignGap = (pair.DoubleValue);
                    break;
                case 47:
                    this.BlockContentXScale = (pair.DoubleValue);
                    break;
                case 49:
                    this.BlockContentYScale = (pair.DoubleValue);
                    break;
                case 90:
                    this.MaxLeaderSegmentCount = (pair.IntegerValue);
                    break;
                case 91:
                    this.LeaderLineColor = (pair.IntegerValue);
                    break;
                case 92:
                    this.LeaderLineWeight = (pair.IntegerValue);
                    break;
                case 93:
                    this.TextColor = (pair.IntegerValue);
                    break;
                case 94:
                    this.BlockContentColor = (pair.IntegerValue);
                    break;
                case 140:
                    this.BlockContentZScale = (pair.DoubleValue);
                    break;
                case 141:
                    this.BlockContentRotation = (pair.DoubleValue);
                    break;
                case 142:
                    this.Scale = (pair.DoubleValue);
                    break;
                case 143:
                    this.BreakGapSize = (pair.DoubleValue);
                    break;
                case 170:
                    this.ContentType = (pair.ShortValue);
                    break;
                case 171:
                    this.DrawMLeaderOrderType = (pair.ShortValue);
                    break;
                case 172:
                    this.DrawLeaderOrderType = (pair.ShortValue);
                    break;
                case 173:
                    this.LeaderLineType = (pair.ShortValue);
                    break;
                case 174:
                    this.TextLeftAttachmentType = (pair.ShortValue);
                    break;
                case 175:
                    this.TextAngleType = (pair.ShortValue);
                    break;
                case 176:
                    this.TextAlignmentType = (pair.ShortValue);
                    break;
                case 177:
                    this.BlockContentConnectionType = (pair.ShortValue);
                    break;
                case 178:
                    this.TextRightAttachmentType = (pair.ShortValue);
                    break;
                case 271:
                    this.TextAttachmentDirection = (DxfTextAttachmentDirection)(pair.ShortValue);
                    break;
                case 272:
                    this.BottomTextAttachmentDirection = (DxfBottomTextAttachmentDirection)(pair.ShortValue);
                    break;
                case 273:
                    this.TopTextAttachmentDirection = (DxfTopTextAttachmentDirection)(pair.ShortValue);
                    break;
                case 290:
                    this.EnableLanding = (pair.BoolValue);
                    break;
                case 291:
                    this.EnableDogleg = (pair.BoolValue);
                    break;
                case 292:
                    this.EnableFrameText = (pair.BoolValue);
                    break;
                case 293:
                    this.EnableBlockContentScale = (pair.BoolValue);
                    break;
                case 294:
                    this.EnableBlockContentRotation = (pair.BoolValue);
                    break;
                case 295:
                    this.OverwritePropertyValue = (pair.BoolValue);
                    break;
                case 296:
                    this.IsAnnotative = (pair.BoolValue);
                    break;
                case 297:
                    this.AlwaysAlignTextLeft = (pair.BoolValue);
                    break;
                case 300:
                    this.DefaultMTextContents = (pair.StringValue);
                    break;
                case 340:
                    this.LineLeaderTypePointer.Handle = DxfCommonConverters.UIntHandle(pair.StringValue);
                    break;
                case 341:
                    this.ArrowheadPointer.Handle = DxfCommonConverters.UIntHandle(pair.StringValue);
                    break;
                case 342:
                    this.MTextStylePointer.Handle = DxfCommonConverters.UIntHandle(pair.StringValue);
                    break;
                case 343:
                    this.BlockContentPointer.Handle = DxfCommonConverters.UIntHandle(pair.StringValue);
                    break;
                default:
                    return base.TrySetPair(pair);
            }

            return true;
        }
    }

}
