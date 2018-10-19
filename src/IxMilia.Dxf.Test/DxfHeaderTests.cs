// Copyright (c) IxMilia.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.IO;
using System.Linq;
using System.Threading;
using IxMilia.Dxf.Entities;
using Xunit;

namespace IxMilia.Dxf.Test
{
    public class DxfHeaderTests : AbstractDxfTests
    {
        #region Read tests

        [Fact]
        public void SpecificHeaderValuesTest()
        {
            var file = Section("HEADER", @"
  9
$ACADMAINTVER
 70
16
  9
$ACADVER
  1
AC1012
  9
$ANGBASE
 50
5.5E1
  9
$ANGDIR
 70
1
  9
$ATTMODE
 70
1
  9
$AUNITS
 70
3
  9
$AUPREC
 70
7
  9
$CLAYER
  8
<current layer>
  9
$LUNITS
 70
6
  9
$LUPREC
 70
7
");
            Assert.Equal(16, file.Header.MaintenenceVersion);
            Assert.Equal(DxfAcadVersion.R13, file.Header.Version);
            Assert.Equal(55.0, file.Header.AngleZeroDirection);
            Assert.Equal(DxfAngleDirection.Clockwise, file.Header.AngleDirection);
            Assert.Equal(DxfAttributeVisibility.Normal, file.Header.AttributeVisibility);
            Assert.Equal(DxfAngleFormat.Radians, file.Header.AngleUnitFormat);
            Assert.Equal(7, file.Header.AngleUnitPrecision);
            Assert.Equal("<current layer>", file.Header.CurrentLayer);
            Assert.Equal(DxfUnitFormat.Architectural, file.Header.UnitFormat);
            Assert.Equal(7, file.Header.UnitPrecision);
        }

        [Fact]
        public void DateConversionTest()
        {
            // from Autodesk spec: 2451544.91568287 = December 31, 1999, 9:58:35 pm.

            // verify reading
            var file = Section("HEADER", @"
  9
$TDCREATE
 40
2451544.91568287
");
            Assert.Equal(new DateTime(1999, 12, 31, 21, 58, 35), file.Header.CreationDate);

            VerifyFileContains(file, @"
  9
$TDCREATE
 40
2451544.91568287
");
        }

        [Fact]
        public void ReadLayerTableTest()
        {
            var file = Section("TABLES", @"
  0
TABLE
  2
LAYER
  0
LAYER
  2
a
 62
12
  0
LAYER
102
{APP_NAME
  1
foo
  2
bar
102
}
  2
b
 62
13
  0
ENDTAB
");
            var layers = file.Layers;
            Assert.Equal(2, layers.Count);
            Assert.Equal("a", layers[0].Name);
            Assert.Equal(12, layers[0].Color.RawValue);
            Assert.Equal("b", layers[1].Name);
            Assert.Equal(13, layers[1].Color.RawValue);

            var group = layers[1].ExtensionDataGroups.Single();
            Assert.Equal("APP_NAME", group.GroupName);
            Assert.Equal(2, group.Items.Count);
            Assert.Equal(new DxfCodePair(1, "foo"), group.Items[0]);
            Assert.Equal(new DxfCodePair(2, "bar"), group.Items[1]);
        }

        [Fact]
        public void WriteLayersTableTest()
        {
            var layer = new DxfLayer("layer-name");
            layer.ExtensionDataGroups.Add(new DxfCodePairGroup("APP_NAME", new IDxfCodePairOrGroup[]
            {
                new DxfCodePair(1, "foo"),
                new DxfCodePair(2, "bar"),
            }));
            var file = new DxfFile();
            file.Layers.Add(layer);
            VerifyFileContains(file, @"
  0
LAYER
  5
#
102
{APP_NAME
  1
foo
  2
bar
102
}
");
        }

        [Fact]
        public void ViewPortTableTest()
        {
            var file = Section("TABLES", @"
  0
TABLE
  2
VPORT
  0
VPORT
  0
VPORT
  2
vport-2
 10
1.100000E+001
 20
2.200000E+001
 11
3.300000E+001
 21
4.400000E+001
 12
5.500000E+001
 22
6.600000E+001
 13
7.700000E+001
 23
8.800000E+001
 14
9.900000E+001
 24
1.200000E+001
 15
1.300000E+001
 25
1.400000E+001
 16
1.500000E+001
 26
1.600000E+001
 36
1.700000E+001
 17
1.800000E+001
 27
1.900000E+001
 37
2.000000E+001
 40
2.100000E+001
 41
2.200000E+001
 42
2.300000E+001
 43
2.400000E+001
 44
2.500000E+001
 50
2.600000E+001
 51
2.700000E+001
  0
ENDTAB
");
            var viewPorts = file.ViewPorts;
            Assert.Equal(2, viewPorts.Count);

            // defaults
            Assert.Equal(null, viewPorts[0].Name);
            Assert.Equal(0.0, viewPorts[0].LowerLeft.X);
            Assert.Equal(0.0, viewPorts[0].LowerLeft.Y);
            Assert.Equal(1.0, viewPorts[0].UpperRight.X);
            Assert.Equal(1.0, viewPorts[0].UpperRight.Y);
            Assert.Equal(0.0, viewPorts[0].ViewCenter.X);
            Assert.Equal(0.0, viewPorts[0].ViewCenter.Y);
            Assert.Equal(0.0, viewPorts[0].SnapBasePoint.X);
            Assert.Equal(0.0, viewPorts[0].SnapBasePoint.Y);
            Assert.Equal(1.0, viewPorts[0].SnapSpacing.X);
            Assert.Equal(1.0, viewPorts[0].SnapSpacing.Y);
            Assert.Equal(1.0, viewPorts[0].GridSpacing.X);
            Assert.Equal(1.0, viewPorts[0].GridSpacing.Y);
            Assert.Equal(0.0, viewPorts[0].ViewDirection.X);
            Assert.Equal(0.0, viewPorts[0].ViewDirection.Y);
            Assert.Equal(1.0, viewPorts[0].ViewDirection.Z);
            Assert.Equal(0.0, viewPorts[0].TargetViewPoint.X);
            Assert.Equal(0.0, viewPorts[0].TargetViewPoint.Y);
            Assert.Equal(0.0, viewPorts[0].TargetViewPoint.Z);
            Assert.Equal(1.0, viewPorts[0].ViewHeight);
            Assert.Equal(1.0, viewPorts[0].ViewPortAspectRatio);
            Assert.Equal(50.0, viewPorts[0].LensLength);
            Assert.Equal(0.0, viewPorts[0].FrontClippingPlane);
            Assert.Equal(0.0, viewPorts[0].BackClippingPlane);
            Assert.Equal(0.0, viewPorts[0].SnapRotationAngle);
            Assert.Equal(0.0, viewPorts[0].ViewTwistAngle);

            // specifics
            Assert.Equal("vport-2", viewPorts[1].Name);
            Assert.Equal(11.0, viewPorts[1].LowerLeft.X);
            Assert.Equal(22.0, viewPorts[1].LowerLeft.Y);
            Assert.Equal(33.0, viewPorts[1].UpperRight.X);
            Assert.Equal(44.0, viewPorts[1].UpperRight.Y);
            Assert.Equal(55.0, viewPorts[1].ViewCenter.X);
            Assert.Equal(66.0, viewPorts[1].ViewCenter.Y);
            Assert.Equal(77.0, viewPorts[1].SnapBasePoint.X);
            Assert.Equal(88.0, viewPorts[1].SnapBasePoint.Y);
            Assert.Equal(99.0, viewPorts[1].SnapSpacing.X);
            Assert.Equal(12.0, viewPorts[1].SnapSpacing.Y);
            Assert.Equal(13.0, viewPorts[1].GridSpacing.X);
            Assert.Equal(14.0, viewPorts[1].GridSpacing.Y);
            Assert.Equal(15.0, viewPorts[1].ViewDirection.X);
            Assert.Equal(16.0, viewPorts[1].ViewDirection.Y);
            Assert.Equal(17.0, viewPorts[1].ViewDirection.Z);
            Assert.Equal(18.0, viewPorts[1].TargetViewPoint.X);
            Assert.Equal(19.0, viewPorts[1].TargetViewPoint.Y);
            Assert.Equal(20.0, viewPorts[1].TargetViewPoint.Z);
            Assert.Equal(21.0, viewPorts[1].ViewHeight);
            Assert.Equal(22.0, viewPorts[1].ViewPortAspectRatio);
            Assert.Equal(23.0, viewPorts[1].LensLength);
            Assert.Equal(24.0, viewPorts[1].FrontClippingPlane);
            Assert.Equal(25.0, viewPorts[1].BackClippingPlane);
            Assert.Equal(26.0, viewPorts[1].SnapRotationAngle);
            Assert.Equal(27.0, viewPorts[1].ViewTwistAngle);
        }

        [Fact]
        public void ReadAlternateVersionTest()
        {
            var file = Section("HEADER", @"
  9
$ACADVER
  1
15.0S
");
            Assert.Equal(DxfAcadVersion.R2000, file.Header.Version);
            Assert.True(file.Header.IsRestrictedVersion);
        }

        [Fact]
        public void ReadEmptyFingerPrintGuidTest()
        {
            var file = Section("HEADER", @"
  9
$FINGERPRINTGUID
2

  9
$ACADVER
  1
AC1012
");
            Assert.Equal(Guid.Empty, file.Header.FingerprintGuid);
        }

        [Fact]
        public void ReadAlternateMaintenenceVersionTest()
        {
            // traditional short value
            var file = Section("HEADER", @"
  9
$ACADMAINTVER
 70
42
");
            Assert.Equal(42, file.Header.MaintenenceVersion);

            // alternate long value
            file = Section("HEADER", @"
  9
$ACADMAINTVER
 90
42
");
            Assert.Equal(42, file.Header.MaintenenceVersion);
        }

        #endregion

        #region Write tests

        [Fact]
        public void WriteDefaultHeaderValuesTest()
        {
            VerifyFileContains(new DxfFile(), @"
  9
$DIMGAP
 40
0.0
");
        }

        [Fact]
        public void WriteSpecificHeaderValuesTest()
        {
            var file = new DxfFile();
            file.Header.DimensionLineGap = 11.0;
            VerifyFileContains(file, @"
  9
$DIMGAP
 40
11.0
");
        }

        [Fact]
        public void WriteLayersTest()
        {
            var file = new DxfFile();
            file.Layers.Add(new DxfLayer("default"));
            VerifyFileContains(file, @"
  0
LAYER
  5
#
100
AcDbSymbolTableRecord
  2
default
 70
0
 62
7
  6

");
        }

        [Fact]
        public void WriteViewportTest()
        {
            var file = new DxfFile();
            file.ViewPorts.Add(new DxfViewPort());
            VerifyFileContains(file, @"
  0
VPORT
  5
3
100
AcDbSymbolTableRecord
  2

 70
0
 10
0.0
 20
0.0
 11
1.0
 21
1.0
 12
0.0
 22
0.0
 13
0.0
 23
0.0
 14
1.0
 24
1.0
 15
1.0
 25
1.0
 16
0.0
 26
0.0
 36
1.0
 17
0.0
 27
0.0
 37
0.0
 40
1.0
 41
1.0
 42
50.0
 43
0.0
 44
0.0
 50
0.0
 51
0.0
 71
0
 72
1000
 73
1
 74
3
 75
0
 76
0
 77
0
 78
0
");
        }

        [Fact]
        public void WriteAndReadTypeDefaultsTest()
        {
            var file = new DxfFile();
            SetAllPropertiesToDefault(file.Header);

            // write each version of the header with default values
            foreach (var version in new[] { DxfAcadVersion.R10, DxfAcadVersion.R11, DxfAcadVersion.R12, DxfAcadVersion.R13, DxfAcadVersion.R14, DxfAcadVersion.R2000, DxfAcadVersion.R2004, DxfAcadVersion.R2007, DxfAcadVersion.R2010, DxfAcadVersion.R2013 })
            {
                file.Header.Version = version;
                using (var ms = new MemoryStream())
                {
                    file.Save(ms);
                    ms.Flush();
                    ms.Seek(0, SeekOrigin.Begin);

                    // ensure the header can be read back in, too
                    var file2 = DxfFile.Load(ms);
                }
            }
        }

        [Fact]
        public void WriteAlternateVersionTest()
        {
            var file = new DxfFile();
            file.Header.Version = DxfAcadVersion.R2000;
            file.Header.IsRestrictedVersion = true;
            VerifyFileContains(file, @"
  9
$ACADVER
  1
AC1015S
");
        }

        [Fact]
        public void HeaderVariablesMatchOfficialR10Order()
        {
            var expectedOrderText = @"
$ACADVER
$INSBASE
$EXTMIN
$EXTMAX
$LIMMIN
$LIMMAX
$ORTHOMODE
$REGENMODE
$FILLMODE
$QTEXTMODE
$MIRRTEXT
$DRAGMODE
$LTSCALE
$OSMODE
$ATTMODE
$TEXTSIZE
$TRACEWID
$TEXTSTYLE
$CLAYER
$CELTYPE
$CECOLOR
$DIMSCALE
$DIMASZ
$DIMEXO
$DIMDLI
$DIMRND
$DIMDLE
$DIMEXE
$DIMTP
$DIMTM
$DIMTXT
$DIMCEN
$DIMTSZ
$DIMTOL
$DIMLIM
$DIMTIH
$DIMTOH
$DIMTAD
$DIMZIN
$DIMBLK
$DIMASO
$DIMSHO
$DIMPOST
$DIMAPOST
$DIMALT
$DIMALTD
$DIMALTF
$DIMLFAC
$DIMTOFL
$DIMTVP
$DIMTIX
$DIMSOXD
$DIMSAH
$DIMBLK1
$DIMBLK2
$DIMSTYLE
$LUNITS
$LUPREC
$SKETCHINC
$FILLETRAD
$AUNITS
$AUPREC
$MENU
$ELEVATION
$THICKNESS
$LIMCHECK
$BLIPMODE
$CHAMFERA
$CHAMFERB
$SKPOLY
$TDCREATE
$TDUPDATE
$TDINDWG
$TDUSRTIMER
$USRTIMER
$ANGBASE
$ANGDIR
$PDMODE
$PDSIZE
$PLINEWID
$COORDS
$SPLFRAME
$SPLINETYPE
$SPLINESEGS
$ATTDIA
$ATTREQ
$HANDLING
$HANDSEED
$SURFTAB1
$SURFTAB2
$SURFTYPE
$SURFU
$SURFV
$UCSNAME
$UCSORG
$UCSXDIR
$UCSYDIR
$USERI1
$USERI2
$USERI3
$USERI4
$USERI5
$USERR1
$USERR2
$USERR3
$USERR4
$USERR5
$WORLDVIEW
$AXISMODE
$AXISUNIT
$FASTZOOM
$GRIDMODE
$GRIDUNIT
$SNAPANG
$SNAPBASE
$SNAPISOPAIR
$SNAPMODE
$SNAPSTYLE
$SNAPUNIT
$VIEWCTR
$VIEWDIR
$VIEWSIZE";
            TestHeaderOrder(expectedOrderText, DxfAcadVersion.R10);
        }

        [Fact]
        public void HeaderVariablesMatchOfficialR12Order()
        {
            var expectedOrderText = @"
$ACADVER
$INSBASE
$EXTMIN
$EXTMAX
$LIMMIN
$LIMMAX
$ORTHOMODE
$REGENMODE
$FILLMODE
$QTEXTMODE
$MIRRTEXT
$DRAGMODE
$LTSCALE
$OSMODE
$ATTMODE
$TEXTSIZE
$TRACEWID
$TEXTSTYLE
$CLAYER
$CELTYPE
$CECOLOR
$DIMSCALE
$DIMASZ
$DIMEXO
$DIMDLI
$DIMRND
$DIMDLE
$DIMEXE
$DIMTP
$DIMTM
$DIMTXT
$DIMCEN
$DIMTSZ
$DIMTOL
$DIMLIM
$DIMTIH
$DIMTOH
$DIMSE1
$DIMSE2
$DIMTAD
$DIMZIN
$DIMBLK
$DIMASO
$DIMSHO
$DIMPOST
$DIMAPOST
$DIMALT
$DIMALTD
$DIMALTF
$DIMLFAC
$DIMTOFL
$DIMTVP
$DIMTIX
$DIMSOXD
$DIMSAH
$DIMBLK1
$DIMBLK2
$DIMSTYLE
$DIMCLRD
$DIMCLRE
$DIMCLRT
$DIMTFAC
$DIMGAP
$LUNITS
$LUPREC
$SKETCHINC
$FILLETRAD
$AUNITS
$AUPREC
$MENU
$ELEVATION
$PELEVATION
$THICKNESS
$LIMCHECK
$BLIPMODE
$CHAMFERA
$CHAMFERB
$SKPOLY
$TDCREATE
$TDUPDATE
$TDINDWG
$TDUSRTIMER
$USRTIMER
$ANGBASE
$ANGDIR
$PDMODE
$PDSIZE
$PLINEWID
$COORDS
$SPLFRAME
$SPLINETYPE
$SPLINESEGS
$ATTDIA
$ATTREQ
$HANDLING
$HANDSEED
$SURFTAB1
$SURFTAB2
$SURFTYPE
$SURFU
$SURFV
$UCSNAME
$UCSORG
$UCSXDIR
$UCSYDIR
$PUCSNAME
$PUCSORG
$PUCSXDIR
$PUCSYDIR
$USERI1
$USERI2
$USERI3
$USERI4
$USERI5
$USERR1
$USERR2
$USERR3
$USERR4
$USERR5
$WORLDVIEW
$SHADEDGE
$SHADEDIF
$TILEMODE
$MAXACTVP
$PLIMCHECK
$PEXTMIN
$PEXTMAX
$PLIMMIN
$PLIMMAX
$UNITMODE
$VISRETAIN
$PLINEGEN
$PSLTSCALE";
            TestHeaderOrder(expectedOrderText, DxfAcadVersion.R12);
        }

        [Fact]
        public void HeaderVariablesMatchOfficialR2000Order()
        {
            var expectedOrderText = @"
$ACADVER
$ACADMAINTVER
$DWGCODEPAGE
$INSBASE
$EXTMIN
$EXTMAX
$LIMMIN
$LIMMAX
$ORTHOMODE
$REGENMODE
$FILLMODE
$QTEXTMODE
$MIRRTEXT
$LTSCALE
$ATTMODE
$TEXTSIZE
$TRACEWID
$TEXTSTYLE
$CLAYER
$CELTYPE
$CECOLOR
$CELTSCALE
$DISPSILH
$DIMSCALE
$DIMASZ
$DIMEXO
$DIMDLI
$DIMRND
$DIMDLE
$DIMEXE
$DIMTP
$DIMTM
$DIMTXT
$DIMCEN
$DIMTSZ
$DIMTOL
$DIMLIM
$DIMTIH
$DIMTOH
$DIMSE1
$DIMSE2
$DIMTAD
$DIMZIN
$DIMBLK
$DIMASO
$DIMSHO
$DIMPOST
$DIMAPOST
$DIMALT
$DIMALTD
$DIMALTF
$DIMLFAC
$DIMTOFL
$DIMTVP
$DIMTIX
$DIMSOXD
$DIMSAH
$DIMBLK1
$DIMBLK2
$DIMSTYLE
$DIMCLRD
$DIMCLRE
$DIMCLRT
$DIMTFAC
$DIMGAP
$DIMJUST
$DIMSD1
$DIMSD2
$DIMTOLJ
$DIMTZIN
$DIMALTZ
$DIMALTTZ
$DIMUPT
$DIMDEC
$DIMTDEC
$DIMALTU
$DIMALTTD
$DIMTXSTY
$DIMAUNIT
$DIMADEC
$DIMALTRND
$DIMAZIN
$DIMDSEP
$DIMATFIT
$DIMFRAC
$DIMLDRBLK
$DIMLUNIT
$DIMLWD
$DIMLWE
$DIMTMOVE
$LUNITS
$LUPREC
$SKETCHINC
$FILLETRAD
$AUNITS
$AUPREC
$MENU
$ELEVATION
$PELEVATION
$THICKNESS
$LIMCHECK
$CHAMFERA
$CHAMFERB
$CHAMFERC
$CHAMFERD
$SKPOLY
$TDCREATE
$TDUCREATE
$TDUPDATE
$TDUUPDATE
$TDINDWG
$TDUSRTIMER
$USRTIMER
$ANGBASE
$ANGDIR
$PDMODE
$PDSIZE
$PLINEWID
$SPLFRAME
$SPLINETYPE
$SPLINESEGS
$HANDSEED
$SURFTAB1
$SURFTAB2
$SURFTYPE
$SURFU
$SURFV
$UCSBASE
$UCSNAME
$UCSORG
$UCSXDIR
$UCSYDIR
$UCSORTHOREF
$UCSORTHOVIEW
$UCSORGTOP
$UCSORGBOTTOM
$UCSORGLEFT
$UCSORGRIGHT
$UCSORGFRONT
$UCSORGBACK
$PUCSBASE
$PUCSNAME
$PUCSORG
$PUCSXDIR
$PUCSYDIR
$PUCSORTHOREF
$PUCSORTHOVIEW
$PUCSORGTOP
$PUCSORGBOTTOM
$PUCSORGLEFT
$PUCSORGRIGHT
$PUCSORGFRONT
$PUCSORGBACK
$USERI1
$USERI2
$USERI3
$USERI4
$USERI5
$USERR1
$USERR2
$USERR3
$USERR4
$USERR5
$WORLDVIEW
$SHADEDGE
$SHADEDIF
$TILEMODE
$MAXACTVP
$PINSBASE
$PLIMCHECK
$PEXTMIN
$PEXTMAX
$PLIMMIN
$PLIMMAX
$UNITMODE
$VISRETAIN
$PLINEGEN
$PSLTSCALE
$TREEDEPTH
$CMLSTYLE
$CMLJUST
$CMLSCALE
$PROXYGRAPHICS
$MEASUREMENT
$CELWEIGHT
$ENDCAPS
$JOINSTYLE
$LWDISPLAY
$INSUNITS
$HYPERLINKBASE
$STYLESHEET
$XEDIT
$CEPSNTYPE
$PSTYLEMODE
$FINGERPRINTGUID
$VERSIONGUID
$EXTNAMES
$PSVPSCALE
$OLESTARTUP";
            TestHeaderOrder(expectedOrderText, DxfAcadVersion.R2000);
        }

        [Fact]
        public void HeaderVariablesMatchOfficialR2004Order()
        {
            var expectedOrderText = @"
$ACADVER
$ACADMAINTVER
$DWGCODEPAGE
$LASTSAVEDBY
$INSBASE
$EXTMIN
$EXTMAX
$LIMMIN
$LIMMAX
$ORTHOMODE
$REGENMODE
$FILLMODE
$QTEXTMODE
$MIRRTEXT
$LTSCALE
$ATTMODE
$TEXTSIZE
$TRACEWID
$TEXTSTYLE
$CLAYER
$CELTYPE
$CECOLOR
$CELTSCALE
$DISPSILH
$DIMSCALE
$DIMASZ
$DIMEXO
$DIMDLI
$DIMRND
$DIMDLE
$DIMEXE
$DIMTP
$DIMTM
$DIMTXT
$DIMCEN
$DIMTSZ
$DIMTOL
$DIMLIM
$DIMTIH
$DIMTOH
$DIMSE1
$DIMSE2
$DIMTAD
$DIMZIN
$DIMBLK
$DIMASO
$DIMSHO
$DIMPOST
$DIMAPOST
$DIMALT
$DIMALTD
$DIMALTF
$DIMLFAC
$DIMTOFL
$DIMTVP
$DIMTIX
$DIMSOXD
$DIMSAH
$DIMBLK1
$DIMBLK2
$DIMSTYLE
$DIMCLRD
$DIMCLRE
$DIMCLRT
$DIMTFAC
$DIMGAP
$DIMJUST
$DIMSD1
$DIMSD2
$DIMTOLJ
$DIMTZIN
$DIMALTZ
$DIMALTTZ
$DIMUPT
$DIMDEC
$DIMTDEC
$DIMALTU
$DIMALTTD
$DIMTXSTY
$DIMAUNIT
$DIMADEC
$DIMALTRND
$DIMAZIN
$DIMDSEP
$DIMATFIT
$DIMFRAC
$DIMLDRBLK
$DIMLUNIT
$DIMLWD
$DIMLWE
$DIMTMOVE
$LUNITS
$LUPREC
$SKETCHINC
$FILLETRAD
$AUNITS
$AUPREC
$MENU
$ELEVATION
$PELEVATION
$THICKNESS
$LIMCHECK
$CHAMFERA
$CHAMFERB
$CHAMFERC
$CHAMFERD
$SKPOLY
$TDCREATE
$TDUCREATE
$TDUPDATE
$TDUUPDATE
$TDINDWG
$TDUSRTIMER
$USRTIMER
$ANGBASE
$ANGDIR
$PDMODE
$PDSIZE
$PLINEWID
$SPLFRAME
$SPLINETYPE
$SPLINESEGS
$HANDSEED
$SURFTAB1
$SURFTAB2
$SURFTYPE
$SURFU
$SURFV
$UCSBASE
$UCSNAME
$UCSORG
$UCSXDIR
$UCSYDIR
$UCSORTHOREF
$UCSORTHOVIEW
$UCSORGTOP
$UCSORGBOTTOM
$UCSORGLEFT
$UCSORGRIGHT
$UCSORGFRONT
$UCSORGBACK
$PUCSBASE
$PUCSNAME
$PUCSORG
$PUCSXDIR
$PUCSYDIR
$PUCSORTHOREF
$PUCSORTHOVIEW
$PUCSORGTOP
$PUCSORGBOTTOM
$PUCSORGLEFT
$PUCSORGRIGHT
$PUCSORGFRONT
$PUCSORGBACK
$USERI1
$USERI2
$USERI3
$USERI4
$USERI5
$USERR1
$USERR2
$USERR3
$USERR4
$USERR5
$WORLDVIEW
$SHADEDGE
$SHADEDIF
$TILEMODE
$MAXACTVP
$PINSBASE
$PLIMCHECK
$PEXTMIN
$PEXTMAX
$PLIMMIN
$PLIMMAX
$UNITMODE
$VISRETAIN
$PLINEGEN
$PSLTSCALE
$TREEDEPTH
$CMLSTYLE
$CMLJUST
$CMLSCALE
$PROXYGRAPHICS
$MEASUREMENT
$CELWEIGHT
$ENDCAPS
$JOINSTYLE
$LWDISPLAY
$INSUNITS
$HYPERLINKBASE
$STYLESHEET
$XEDIT
$CEPSNTYPE
$PSTYLEMODE
$FINGERPRINTGUID
$VERSIONGUID
$EXTNAMES
$PSVPSCALE
$OLESTARTUP
$SORTENTS
$INDEXCTL
$HIDETEXT
$XCLIPFRAME
$HALOGAP
$OBSCOLOR
$OBSLTYPE
$INTERSECTIONDISPLAY
$INTERSECTIONCOLOR
$DIMASSOC
$PROJECTNAME";
            TestHeaderOrder(expectedOrderText, DxfAcadVersion.R2004);
        }

        [Fact]
        public void HeaderVariablesMatchOfficialR2007Order()
        {
            var expectedOrderText = @"
$ACADVER
$ACADMAINTVER
$DWGCODEPAGE
$LASTSAVEDBY
$INSBASE
$EXTMIN
$EXTMAX
$LIMMIN
$LIMMAX
$ORTHOMODE
$REGENMODE
$FILLMODE
$QTEXTMODE
$MIRRTEXT
$LTSCALE
$ATTMODE
$TEXTSIZE
$TRACEWID
$TEXTSTYLE
$CLAYER
$CELTYPE
$CECOLOR
$CELTSCALE
$DISPSILH
$DIMSCALE
$DIMASZ
$DIMEXO
$DIMDLI
$DIMRND
$DIMDLE
$DIMEXE
$DIMTP
$DIMTM
$DIMTXT
$DIMCEN
$DIMTSZ
$DIMTOL
$DIMLIM
$DIMTIH
$DIMTOH
$DIMSE1
$DIMSE2
$DIMTAD
$DIMZIN
$DIMBLK
$DIMASO
$DIMSHO
$DIMPOST
$DIMAPOST
$DIMALT
$DIMALTD
$DIMALTF
$DIMLFAC
$DIMTOFL
$DIMTVP
$DIMTIX
$DIMSOXD
$DIMSAH
$DIMBLK1
$DIMBLK2
$DIMSTYLE
$DIMCLRD
$DIMCLRE
$DIMCLRT
$DIMTFAC
$DIMGAP
$DIMJUST
$DIMSD1
$DIMSD2
$DIMTOLJ
$DIMTZIN
$DIMALTZ
$DIMALTTZ
$DIMUPT
$DIMDEC
$DIMTDEC
$DIMALTU
$DIMALTTD
$DIMTXSTY
$DIMAUNIT
$DIMADEC
$DIMALTRND
$DIMAZIN
$DIMDSEP
$DIMATFIT
$DIMFRAC
$DIMLDRBLK
$DIMLUNIT
$DIMLWD
$DIMLWE
$DIMTMOVE
$DIMFXL
$DIMFXLON
$DIMJOGANG
$DIMTFILL
$DIMTFILLCLR
$DIMARCSYM
$DIMLTYPE
$DIMLTEX1
$DIMLTEX2
$LUNITS
$LUPREC
$SKETCHINC
$FILLETRAD
$AUNITS
$AUPREC
$MENU
$ELEVATION
$PELEVATION
$THICKNESS
$LIMCHECK
$CHAMFERA
$CHAMFERB
$CHAMFERC
$CHAMFERD
$SKPOLY
$TDCREATE
$TDUCREATE
$TDUPDATE
$TDUUPDATE
$TDINDWG
$TDUSRTIMER
$USRTIMER
$ANGBASE
$ANGDIR
$PDMODE
$PDSIZE
$PLINEWID
$SPLFRAME
$SPLINETYPE
$SPLINESEGS
$HANDSEED
$SURFTAB1
$SURFTAB2
$SURFTYPE
$SURFU
$SURFV
$UCSBASE
$UCSNAME
$UCSORG
$UCSXDIR
$UCSYDIR
$UCSORTHOREF
$UCSORTHOVIEW
$UCSORGTOP
$UCSORGBOTTOM
$UCSORGLEFT
$UCSORGRIGHT
$UCSORGFRONT
$UCSORGBACK
$PUCSBASE
$PUCSNAME
$PUCSORG
$PUCSXDIR
$PUCSYDIR
$PUCSORTHOREF
$PUCSORTHOVIEW
$PUCSORGTOP
$PUCSORGBOTTOM
$PUCSORGLEFT
$PUCSORGRIGHT
$PUCSORGFRONT
$PUCSORGBACK
$USERI1
$USERI2
$USERI3
$USERI4
$USERI5
$USERR1
$USERR2
$USERR3
$USERR4
$USERR5
$WORLDVIEW
$SHADEDGE
$SHADEDIF
$TILEMODE
$MAXACTVP
$PINSBASE
$PLIMCHECK
$PEXTMIN
$PEXTMAX
$PLIMMIN
$PLIMMAX
$UNITMODE
$VISRETAIN
$PLINEGEN
$PSLTSCALE
$TREEDEPTH
$CMLSTYLE
$CMLJUST
$CMLSCALE
$PROXYGRAPHICS
$MEASUREMENT
$CELWEIGHT
$ENDCAPS
$JOINSTYLE
$LWDISPLAY
$INSUNITS
$HYPERLINKBASE
$STYLESHEET
$XEDIT
$CEPSNTYPE
$PSTYLEMODE
$FINGERPRINTGUID
$VERSIONGUID
$EXTNAMES
$PSVPSCALE
$OLESTARTUP
$SORTENTS
$INDEXCTL
$HIDETEXT
$XCLIPFRAME
$HALOGAP
$OBSCOLOR
$OBSLTYPE
$INTERSECTIONDISPLAY
$INTERSECTIONCOLOR
$DIMASSOC
$PROJECTNAME
$CAMERADISPLAY
$LENSLENGTH
$CAMERAHEIGHT
$STEPSPERSEC
$STEPSIZE
$3DDWFPREC
$PSOLWIDTH
$PSOLHEIGHT
$LOFTANG1
$LOFTANG2
$LOFTMAG1
$LOFTMAG2
$LOFTPARAM
$LOFTNORMALS
$LATITUDE
$LONGITUDE
$NORTHDIRECTION
$TIMEZONE
$LIGHTGLYPHDISPLAY
$TILEMODELIGHTSYNCH
$CMATERIAL
$SOLIDHIST
$SHOWHIST
$DWFFRAME
$DGNFRAME
$REALWORLDSCALE
$INTERFERECOLOR
$INTERFEREOBJVS
$INTERFEREVPVS
$CSHADOW
$SHADOWPLANELOCATION";
            TestHeaderOrder(expectedOrderText, DxfAcadVersion.R2007);
        }

        [Fact]
        public void HeaderVariablesMatchOfficialR2010Order()
        {
            var expectedOrderText = @"
$ACADVER
$ACADMAINTVER
$DWGCODEPAGE
$LASTSAVEDBY
$INSBASE
$EXTMIN
$EXTMAX
$LIMMIN
$LIMMAX
$ORTHOMODE
$REGENMODE
$FILLMODE
$QTEXTMODE
$MIRRTEXT
$LTSCALE
$ATTMODE
$TEXTSIZE
$TRACEWID
$TEXTSTYLE
$CLAYER
$CELTYPE
$CECOLOR
$CELTSCALE
$DISPSILH
$DIMSCALE
$DIMASZ
$DIMEXO
$DIMDLI
$DIMRND
$DIMDLE
$DIMEXE
$DIMTP
$DIMTM
$DIMTXT
$DIMCEN
$DIMTSZ
$DIMTOL
$DIMLIM
$DIMTIH
$DIMTOH
$DIMSE1
$DIMSE2
$DIMTAD
$DIMZIN
$DIMBLK
$DIMASO
$DIMSHO
$DIMPOST
$DIMAPOST
$DIMALT
$DIMALTD
$DIMALTF
$DIMLFAC
$DIMTOFL
$DIMTVP
$DIMTIX
$DIMSOXD
$DIMSAH
$DIMBLK1
$DIMBLK2
$DIMSTYLE
$DIMCLRD
$DIMCLRE
$DIMCLRT
$DIMTFAC
$DIMGAP
$DIMJUST
$DIMSD1
$DIMSD2
$DIMTOLJ
$DIMTZIN
$DIMALTZ
$DIMALTTZ
$DIMUPT
$DIMDEC
$DIMTDEC
$DIMALTU
$DIMALTTD
$DIMTXSTY
$DIMAUNIT
$DIMADEC
$DIMALTRND
$DIMAZIN
$DIMDSEP
$DIMATFIT
$DIMFRAC
$DIMLDRBLK
$DIMLUNIT
$DIMLWD
$DIMLWE
$DIMTMOVE
$DIMFXL
$DIMFXLON
$DIMJOGANG
$DIMTFILL
$DIMTFILLCLR
$DIMARCSYM
$DIMLTYPE
$DIMLTEX1
$DIMLTEX2
$DIMTXTDIRECTION
$LUNITS
$LUPREC
$SKETCHINC
$FILLETRAD
$AUNITS
$AUPREC
$MENU
$ELEVATION
$PELEVATION
$THICKNESS
$LIMCHECK
$CHAMFERA
$CHAMFERB
$CHAMFERC
$CHAMFERD
$SKPOLY
$TDCREATE
$TDUCREATE
$TDUPDATE
$TDUUPDATE
$TDINDWG
$TDUSRTIMER
$USRTIMER
$ANGBASE
$ANGDIR
$PDMODE
$PDSIZE
$PLINEWID
$SPLFRAME
$SPLINETYPE
$SPLINESEGS
$HANDSEED
$SURFTAB1
$SURFTAB2
$SURFTYPE
$SURFU
$SURFV
$UCSBASE
$UCSNAME
$UCSORG
$UCSXDIR
$UCSYDIR
$UCSORTHOREF
$UCSORTHOVIEW
$UCSORGTOP
$UCSORGBOTTOM
$UCSORGLEFT
$UCSORGRIGHT
$UCSORGFRONT
$UCSORGBACK
$PUCSBASE
$PUCSNAME
$PUCSORG
$PUCSXDIR
$PUCSYDIR
$PUCSORTHOREF
$PUCSORTHOVIEW
$PUCSORGTOP
$PUCSORGBOTTOM
$PUCSORGLEFT
$PUCSORGRIGHT
$PUCSORGFRONT
$PUCSORGBACK
$USERI1
$USERI2
$USERI3
$USERI4
$USERI5
$USERR1
$USERR2
$USERR3
$USERR4
$USERR5
$WORLDVIEW
$SHADEDGE
$SHADEDIF
$TILEMODE
$MAXACTVP
$PINSBASE
$PLIMCHECK
$PEXTMIN
$PEXTMAX
$PLIMMIN
$PLIMMAX
$UNITMODE
$VISRETAIN
$PLINEGEN
$PSLTSCALE
$TREEDEPTH
$CMLSTYLE
$CMLJUST
$CMLSCALE
$PROXYGRAPHICS
$MEASUREMENT
$CELWEIGHT
$ENDCAPS
$JOINSTYLE
$LWDISPLAY
$INSUNITS
$HYPERLINKBASE
$STYLESHEET
$XEDIT
$CEPSNTYPE
$PSTYLEMODE
$FINGERPRINTGUID
$VERSIONGUID
$EXTNAMES
$PSVPSCALE
$OLESTARTUP
$SORTENTS
$INDEXCTL
$HIDETEXT
$XCLIPFRAME
$HALOGAP
$OBSCOLOR
$OBSLTYPE
$INTERSECTIONDISPLAY
$INTERSECTIONCOLOR
$DIMASSOC
$PROJECTNAME
$CAMERADISPLAY
$LENSLENGTH
$CAMERAHEIGHT
$STEPSPERSEC
$STEPSIZE
$3DDWFPREC
$PSOLWIDTH
$PSOLHEIGHT
$LOFTANG1
$LOFTANG2
$LOFTMAG1
$LOFTMAG2
$LOFTPARAM
$LOFTNORMALS
$LATITUDE
$LONGITUDE
$NORTHDIRECTION
$TIMEZONE
$LIGHTGLYPHDISPLAY
$TILEMODELIGHTSYNCH
$CMATERIAL
$SOLIDHIST
$SHOWHIST
$DWFFRAME
$DGNFRAME
$REALWORLDSCALE
$INTERFERECOLOR
$INTERFEREOBJVS
$INTERFEREVPVS
$CSHADOW
$SHADOWPLANELOCATION";
            TestHeaderOrder(expectedOrderText, DxfAcadVersion.R2010);
        }

        [Fact]
        public void HeaderVariablesMatchOfficialR2013Order()
        {
            var expectedOrderText = @"
$ACADVER
$ACADMAINTVER
$DWGCODEPAGE
$LASTSAVEDBY
$REQUIREDVERSIONS
$INSBASE
$EXTMIN
$EXTMAX
$LIMMIN
$LIMMAX
$ORTHOMODE
$REGENMODE
$FILLMODE
$QTEXTMODE
$MIRRTEXT
$LTSCALE
$ATTMODE
$TEXTSIZE
$TRACEWID
$TEXTSTYLE
$CLAYER
$CELTYPE
$CECOLOR
$CELTSCALE
$DISPSILH
$DIMSCALE
$DIMASZ
$DIMEXO
$DIMDLI
$DIMRND
$DIMDLE
$DIMEXE
$DIMTP
$DIMTM
$DIMTXT
$DIMCEN
$DIMTSZ
$DIMTOL
$DIMLIM
$DIMTIH
$DIMTOH
$DIMSE1
$DIMSE2
$DIMTAD
$DIMZIN
$DIMBLK
$DIMASO
$DIMSHO
$DIMPOST
$DIMAPOST
$DIMALT
$DIMALTD
$DIMALTF
$DIMLFAC
$DIMTOFL
$DIMTVP
$DIMTIX
$DIMSOXD
$DIMSAH
$DIMBLK1
$DIMBLK2
$DIMSTYLE
$DIMCLRD
$DIMCLRE
$DIMCLRT
$DIMTFAC
$DIMGAP
$DIMJUST
$DIMSD1
$DIMSD2
$DIMTOLJ
$DIMTZIN
$DIMALTZ
$DIMALTTZ
$DIMUPT
$DIMDEC
$DIMTDEC
$DIMALTU
$DIMALTTD
$DIMTXSTY
$DIMAUNIT
$DIMADEC
$DIMALTRND
$DIMAZIN
$DIMDSEP
$DIMATFIT
$DIMFRAC
$DIMLDRBLK
$DIMLUNIT
$DIMLWD
$DIMLWE
$DIMTMOVE
$DIMFXL
$DIMFXLON
$DIMJOGANG
$DIMTFILL
$DIMTFILLCLR
$DIMARCSYM
$DIMLTYPE
$DIMLTEX1
$DIMLTEX2
$DIMTXTDIRECTION
$LUNITS
$LUPREC
$SKETCHINC
$FILLETRAD
$AUNITS
$AUPREC
$MENU
$ELEVATION
$PELEVATION
$THICKNESS
$LIMCHECK
$CHAMFERA
$CHAMFERB
$CHAMFERC
$CHAMFERD
$SKPOLY
$TDCREATE
$TDUCREATE
$TDUPDATE
$TDUUPDATE
$TDINDWG
$TDUSRTIMER
$USRTIMER
$ANGBASE
$ANGDIR
$PDMODE
$PDSIZE
$PLINEWID
$SPLFRAME
$SPLINETYPE
$SPLINESEGS
$HANDSEED
$SURFTAB1
$SURFTAB2
$SURFTYPE
$SURFU
$SURFV
$UCSBASE
$UCSNAME
$UCSORG
$UCSXDIR
$UCSYDIR
$UCSORTHOREF
$UCSORTHOVIEW
$UCSORGTOP
$UCSORGBOTTOM
$UCSORGLEFT
$UCSORGRIGHT
$UCSORGFRONT
$UCSORGBACK
$PUCSBASE
$PUCSNAME
$PUCSORG
$PUCSXDIR
$PUCSYDIR
$PUCSORTHOREF
$PUCSORTHOVIEW
$PUCSORGTOP
$PUCSORGBOTTOM
$PUCSORGLEFT
$PUCSORGRIGHT
$PUCSORGFRONT
$PUCSORGBACK
$USERI1
$USERI2
$USERI3
$USERI4
$USERI5
$USERR1
$USERR2
$USERR3
$USERR4
$USERR5
$WORLDVIEW
$SHADEDGE
$SHADEDIF
$TILEMODE
$MAXACTVP
$PINSBASE
$PLIMCHECK
$PEXTMIN
$PEXTMAX
$PLIMMIN
$PLIMMAX
$UNITMODE
$VISRETAIN
$PLINEGEN
$PSLTSCALE
$TREEDEPTH
$CMLSTYLE
$CMLJUST
$CMLSCALE
$PROXYGRAPHICS
$MEASUREMENT
$CELWEIGHT
$ENDCAPS
$JOINSTYLE
$LWDISPLAY
$INSUNITS
$HYPERLINKBASE
$STYLESHEET
$XEDIT
$CEPSNTYPE
$PSTYLEMODE
$FINGERPRINTGUID
$VERSIONGUID
$EXTNAMES
$PSVPSCALE
$OLESTARTUP
$SORTENTS
$INDEXCTL
$HIDETEXT
$XCLIPFRAME
$HALOGAP
$OBSCOLOR
$OBSLTYPE
$INTERSECTIONDISPLAY
$INTERSECTIONCOLOR
$DIMASSOC
$PROJECTNAME
$CAMERADISPLAY
$LENSLENGTH
$CAMERAHEIGHT
$STEPSPERSEC
$STEPSIZE
$3DDWFPREC
$PSOLWIDTH
$PSOLHEIGHT
$LOFTANG1
$LOFTANG2
$LOFTMAG1
$LOFTMAG2
$LOFTPARAM
$LOFTNORMALS
$LATITUDE
$LONGITUDE
$NORTHDIRECTION
$TIMEZONE
$LIGHTGLYPHDISPLAY
$TILEMODELIGHTSYNCH
$CMATERIAL
$SOLIDHIST
$SHOWHIST
$DWFFRAME
$DGNFRAME
$REALWORLDSCALE
$INTERFERECOLOR
$INTERFEREOBJVS
$INTERFEREVPVS
$CSHADOW
$SHADOWPLANELOCATION";
            TestHeaderOrder(expectedOrderText, DxfAcadVersion.R2013);
        }

        [Fact]
        public void DontWriteNullPointersTest()
        {
            var file = new DxfFile();

            // ensure they default to 0
            Assert.Equal(0u, file.Header.CurrentMaterialHandle);
            Assert.Equal(0u, file.Header.InterferenceObjectVisualStylePointer);
            Assert.Equal(0u, file.Header.InterferenceViewPortVisualStylePointer);

            var output = ToString(file);
            Assert.DoesNotContain("$CMATERIAL", output);
            Assert.DoesNotContain("$INTERFEREOBJVS", output);
            Assert.DoesNotContain("$INTERFEREVPVS", output);
        }

        private static void TestHeaderOrder(string expectedOrderText, DxfAcadVersion version)
        {
            var file = new DxfFile();
            file.Header.Version = version;
            file.Header.CurrentMaterialHandle = 100u;
            file.Header.InterferenceObjectVisualStylePointer = 101u;
            file.Header.InterferenceViewPortVisualStylePointer = 102u;
            var contents = ToString(file);
            var headerEndSec = contents.IndexOf("ENDSEC");
            contents = contents.Substring(0, headerEndSec);
            var expectedOrder = expectedOrderText
                .Split("\n".ToCharArray(), StringSplitOptions.RemoveEmptyEntries)
                .Where(s => s.StartsWith("$"))
                .Select(s => s.Trim())
                .ToArray();
            var actualOrder = contents
                .Split("\n".ToCharArray(), StringSplitOptions.RemoveEmptyEntries)
                .Where(s => s.StartsWith("$"))
                .Select(s => s.Trim())
                .ToArray();
            AssertArrayEqual(expectedOrder, actualOrder);
        }

        #endregion

        #region Other tests

        [Fact]
        public void TimersTest()
        {
            var file = new DxfFile();
            Thread.Sleep(TimeSpan.FromMilliseconds(20));
            using (var ms = new MemoryStream())
            {
                // we don't really care what's written but this will force the timers to be updated
                file.Save(ms);
            }

            Assert.True(file.Header.TimeInDrawing >= TimeSpan.FromMilliseconds(20));
        }

        [Fact]
        public void DefaultValuesTest()
        {
            var file = new DxfFile();
            Assert.True(file.Header.AlignDirection);
            Assert.Equal(2, file.Header.AlternateDimensioningDecimalPlaces);
            Assert.Equal(25.4, file.Header.AlternateDimensioningScaleFactor);
            Assert.Equal(null, file.Header.AlternateDimensioningSuffix);
            Assert.Equal(2, file.Header.AlternateDimensioningToleranceDecimalPlaces);
            Assert.Equal(DxfUnitZeroSuppression.SuppressZeroFeetAndZeroInches, file.Header.AlternateDimensioningToleranceZeroSupression);
            Assert.Equal(0, file.Header.AlternateDimensioningUnitRounding);
            Assert.Equal(DxfUnitFormat.Decimal, file.Header.AlternateDimensioningUnits);
            Assert.Equal(DxfUnitZeroSuppression.SuppressZeroFeetAndZeroInches, file.Header.AlternateDimensioningZeroSupression);
            Assert.Equal(0, file.Header.AngleBetweenYAxisAndNorth);
            Assert.Equal(DxfAngleDirection.CounterClockwise, file.Header.AngleDirection);
            Assert.Equal(DxfAngleFormat.DecimalDegrees, file.Header.AngleUnitFormat);
            Assert.Equal(0, file.Header.AngleUnitPrecision);
            Assert.Equal(0, file.Header.AngleZeroDirection);
            Assert.Equal(0, file.Header.AngularDimensionPrecision);
            Assert.False(file.Header.ApparentIntersectionSnap);
            Assert.Equal(null, file.Header.ArrowBlockName);
            Assert.Equal(DxfAttributeVisibility.Normal, file.Header.AttributeVisibility);
            Assert.False(file.Header.AxisOn);
            Assert.Equal(new DxfVector(0, 0, 0), file.Header.AxisTickSpacing);
            Assert.False(file.Header.BlipMode);
            Assert.Equal(0, file.Header.CameraHeight);
            Assert.True(file.Header.CanUseInPlaceReferenceEditing);
            Assert.Equal(0.09, file.Header.CenterMarkSize);
            Assert.True(file.Header.CenterSnap);
            Assert.Equal(0, file.Header.ChamferAngle);
            Assert.Equal(0, file.Header.ChamferLength);
            Assert.False(file.Header.Close);
            Assert.Equal(DxfCoordinateDisplay.ContinuousUpdate, file.Header.CoordinateDisplay);
            Assert.True(file.Header.CreateAssociativeDimensioning);
            Assert.Equal(DxfColor.ByLayer, file.Header.CurrentEntityColor);
            Assert.Equal("BYLAYER", file.Header.CurrentEntityLineType);
            Assert.Equal(1, file.Header.CurrentEntityLineTypeScale);
            Assert.Equal("0", file.Header.CurrentLayer);
            Assert.Equal(0u, file.Header.CurrentMaterialHandle);
            Assert.Equal(DxfJustification.Top, file.Header.CurrentMultilineJustification);
            Assert.Equal(1, file.Header.CurrentMultilineScale);
            Assert.Equal("STANDARD", file.Header.CurrentMultilineStyle);
            Assert.Equal(DxfUnits.Unitless, file.Header.DefaultDrawingUnits);
            Assert.Equal(0, file.Header.DefaultPolylineWidth);
            Assert.Equal(0.2, file.Header.DefaultTextHeight);
            Assert.Equal(DxfUnderlayFrameMode.None, file.Header.DgnUnderlayFrameMode);
            Assert.Equal(DxfUnitZeroSuppression.SuppressZeroFeetAndZeroInches, file.Header.DimensionAngleZeroSuppression);
            Assert.Equal(DxfDimensionArcSymbolDisplayMode.SymbolBeforeText, file.Header.DimensionArcSymbolDisplayMode);
            Assert.False(file.Header.DimensionCursorControlsTextPosition);
            Assert.Equal('.', file.Header.DimensionDecimalSeparatorChar);
            Assert.Equal(0, file.Header.DimensionDistanceRoundingValue);
            Assert.Equal(DxfColor.ByBlock, file.Header.DimensionExtensionLineColor);
            Assert.Equal(0.18, file.Header.DimensionExtensionLineExtension);
            Assert.Equal(0.0625, file.Header.DimensionExtensionLineOffset);
            Assert.Equal(DxfLineWeight.ByLayer, file.Header.DimensionExtensionLineWeight);
            Assert.Equal(null, file.Header.DimensionFirstExtensionLineType);
            Assert.Equal(DxfAngleFormat.DecimalDegrees, file.Header.DimensioningAngleFormat);
            Assert.Equal(0.18, file.Header.DimensioningArrowSize);
            Assert.Equal(1, file.Header.DimensioningScaleFactor);
            Assert.Equal(null, file.Header.DimensioningSuffix);
            Assert.Equal(0.18, file.Header.DimensioningTextHeight);
            Assert.Equal(0, file.Header.DimensioningTickSize);
            Assert.Equal(null, file.Header.DimensionLeaderBlockName);
            Assert.Equal(1, file.Header.DimensionLinearMeasurementsScaleFactor);
            Assert.Equal(DxfColor.ByBlock, file.Header.DimensionLineColor);
            Assert.Equal(0, file.Header.DimensionLineExtension);
            Assert.Equal(1, file.Header.DimensionLineFixedLength);
            Assert.False(file.Header.DimensionLineFixedLengthOn);
            Assert.Equal(0.09, file.Header.DimensionLineGap);
            Assert.Equal(0.38, file.Header.DimensionLineIncrement);
            Assert.Equal(null, file.Header.DimensionLineType);
            Assert.Equal(DxfLineWeight.ByLayer, file.Header.DimensionLineWeight);
            Assert.Equal(0, file.Header.DimensionMinusTolerance);
            Assert.Equal(DxfNonAngularUnits.Decimal, file.Header.DimensionNonAngularUnits);
            Assert.Equal(DxfDimensionAssociativity.NonAssociativeObjects, file.Header.DimensionObjectAssociativity);
            Assert.Equal(0, file.Header.DimensionPlusTolerance);
            Assert.Equal(null, file.Header.DimensionSecondExtensionLineType);
            Assert.Equal("STANDARD", file.Header.DimensionStyleName);
            Assert.Equal(DxfDimensionFit.TextAndArrowsOutsideLines, file.Header.DimensionTextAndArrowPlacement);
            Assert.Equal(DxfDimensionTextBackgroundColorMode.None, file.Header.DimensionTextBackgroundColorMode);
            Assert.Equal(DxfColor.ByBlock, file.Header.DimensionTextColor);
            Assert.Equal(DxfTextDirection.LeftToRight, file.Header.DimensionTextDirection);
            Assert.Equal(DxfDimensionFractionFormat.HorizontalStacking, file.Header.DimensionTextHeightScaleFactor);
            Assert.True(file.Header.DimensionTextInsideHorizontal);
            Assert.Equal(DxfDimensionTextJustification.AboveLineCenter, file.Header.DimensionTextJustification);
            Assert.Equal(DxfDimensionTextMovementRule.MoveLineWithText, file.Header.DimensionTextMovementRule);
            Assert.True(file.Header.DimensionTextOutsideHorizontal);
            Assert.Equal("STANDARD", file.Header.DimensionTextStyle);
            Assert.Equal(4, file.Header.DimensionToleranceDecimalPlaces);
            Assert.Equal(1, file.Header.DimensionToleranceDisplayScaleFactor);
            Assert.Equal(DxfJustification.Middle, file.Header.DimensionToleranceVerticalJustification);
            Assert.Equal(DxfUnitZeroSuppression.SuppressZeroFeetAndZeroInches, file.Header.DimensionToleranceZeroSuppression);
            Assert.Equal(Math.PI / 4.0, file.Header.DimensionTransverseSegmentAngleInJoggedRadius);
            Assert.Equal(DxfUnitFormat.Decimal, file.Header.DimensionUnitFormat);
            Assert.Equal(4, file.Header.DimensionUnitToleranceDecimalPlaces);
            Assert.Equal(DxfUnitZeroSuppression.SuppressZeroFeetAndZeroInches, file.Header.DimensionUnitZeroSuppression);
            Assert.Equal(0, file.Header.DimensionVerticalTextPosition);
            Assert.False(file.Header.DisplayFractionsInInput);
            Assert.False(file.Header.DisplayIntersectionPolylines);
            Assert.False(file.Header.DisplayLinewieghtInModelAndLayoutTab);
            Assert.False(file.Header.DisplaySilhouetteCurvesInWireframeMode);
            Assert.False(file.Header.DisplaySplinePolygonControl);
            Assert.Equal(DxfDragMode.Auto, file.Header.DragMode);
            Assert.Equal("ANSI_1252", file.Header.DrawingCodePage);
            Assert.Equal(DxfDrawingUnits.English, file.Header.DrawingUnits);
            Assert.False(file.Header.DrawOrthoganalLines);
            Assert.Equal(Dxf3DDwfPrecision.Deviation_0_5, file.Header.Dwf3DPrecision);
            Assert.Equal(DxfUnderlayFrameMode.DisplayNoPlot, file.Header.DwfUnderlayFrameMode);
            Assert.Equal(DxfColor.ByBlock, file.Header.DxfDimensionTextBackgroundCustomColor);
            Assert.Equal(DxfShadeEdgeMode.FacesInEntityColorEdgesInBlack, file.Header.EdgeShading);
            Assert.Equal(0, file.Header.Elevation);
            Assert.Equal(DxfEndCapSetting.None, file.Header.EndCapSetting);
            Assert.True(file.Header.EndPointSnap);
            Assert.False(file.Header.ExtensionSnap);
            Assert.True(file.Header.FastZoom);
            Assert.Equal(".", file.Header.FileName);
            Assert.Equal(0, file.Header.FilletRadius);
            Assert.True(file.Header.FillModeOn);
            Assert.Equal(null, file.Header.FirstArrowBlockName);
            Assert.Equal(0, file.Header.FirstChamferDistance);
            Assert.False(file.Header.ForceDimensionLineExtensionsOutsideIfTextIs);
            Assert.False(file.Header.ForceDimensionTextInsideExtensions);
            Assert.False(file.Header.GenerateDimensionLimits);
            Assert.False(file.Header.GenerateDimensionTolerances);
            Assert.False(file.Header.GridOn);
            Assert.Equal(new DxfVector(1, 1, 0), file.Header.GridSpacing);
            Assert.Equal(0, file.Header.HaloGapPercent);
            Assert.True(file.Header.HandlesEnabled);
            Assert.False(file.Header.HideTextObjectsWhenProducintHiddenView);
            Assert.Equal(null, file.Header.HyperlinkBase);
            Assert.Equal(new DxfPoint(0, 0, 0), file.Header.InsertionBase);
            Assert.False(file.Header.InsertionSnap);
            Assert.Equal(1, file.Header.InterferenceObjectColor.Index);
            Assert.Equal(0u, file.Header.InterferenceObjectVisualStylePointer);
            Assert.Equal(0u, file.Header.InterferenceViewPortVisualStylePointer);
            Assert.Equal(DxfColor.ByEntity, file.Header.IntersectionPolylineColor);
            Assert.True(file.Header.IntersectionSnap);
            Assert.False(file.Header.IsPolylineContinuousAroundVerticies);
            Assert.False(file.Header.IsRestrictedVersion);
            Assert.True(file.Header.IsViewportScaledToFit);
            Assert.Equal(DxfXrefClippingBoundaryVisibility.DisplayedNotPlotted, file.Header.IsXRefClippingBoundaryVisible);
            Assert.Equal(4, file.Header.LastPolySolidHeight);
            Assert.Equal(0.25, file.Header.LastPolySolidWidth);
            Assert.Equal(null, file.Header.LastSavedBy);
            Assert.Equal(37.795, file.Header.Latitude);
            Assert.Equal(DxfLayerAndSpatialIndexSaveMode.None, file.Header.LayerAndSpatialIndexSaveMode);
            Assert.Equal(50, file.Header.LensLength);
            Assert.False(file.Header.LimitCheckingInPaperspace);
            Assert.Equal(8, file.Header.LineSegmentsPerSplinePatch);
            Assert.Equal(1, file.Header.LineTypeScale);
            Assert.Equal(DxfJoinStyle.None, file.Header.LineweightJointSetting);
            Assert.Equal(DxfLoftedObjectNormalMode.SmoothFit, file.Header.LoftedObjectNormalMode);
            Assert.Equal(7, file.Header.LoftFlags);
            Assert.Equal(Math.PI / 2.0, file.Header.LoftOperationFirstDraftAngle);
            Assert.Equal(0, file.Header.LoftOperationFirstMagnitude);
            Assert.Equal(Math.PI / 2.0, file.Header.LoftOperationSecondDraftAngle);
            Assert.Equal(0, file.Header.LoftOperationSecondMagnitude);
            Assert.Equal(-122.394, file.Header.Longitude);
            Assert.Equal(0, file.Header.MaintenenceVersion);
            Assert.Equal(64, file.Header.MaximumActiveViewports);
            Assert.Equal(new DxfPoint(0, 0, 0), file.Header.MaximumDrawingExtents);
            Assert.Equal(new DxfPoint(12, 9, 0), file.Header.MaximumDrawingLimits);
            Assert.Equal(6, file.Header.MeshTabulationsInFirstDirection);
            Assert.Equal(6, file.Header.MeshTabulationsInSecondDirection);
            Assert.False(file.Header.MidPointSnap);
            Assert.Equal(new DxfPoint(0, 0, 0), file.Header.MinimumDrawingExtents);
            Assert.Equal(new DxfPoint(0, 0, 0), file.Header.MinimumDrawingLimits);
            Assert.False(file.Header.MirrorText);
            Assert.False(file.Header.NearestSnap);
            Assert.Equal(DxfLineWeight.ByBlock, file.Header.NewObjectLineWeight);
            Assert.Equal(DxfPlotStyle.ByLayer, file.Header.NewObjectPlotStyle);
            Assert.False(file.Header.NewSolidsContainHistory);
            Assert.Equal(0u, file.Header.NextAvailableHandle);
            Assert.False(file.Header.NodeSnap);
            Assert.True(file.Header.NoTwist);
            Assert.Equal(37, file.Header.ObjectSnapFlags);
            Assert.Equal(127, file.Header.ObjectSortingMethodsFlags);
            Assert.Equal(DxfColor.ByEntity, file.Header.ObscuredLineColor);
            Assert.Equal(DxfLineTypeStyle.Off, file.Header.ObscuredLineTypeStyle);
            Assert.False(file.Header.OleStartup);
            Assert.Equal(DxfOrthographicViewType.None, file.Header.OrthgraphicViewType);
            Assert.Equal(null, file.Header.OrthoUCSReference);
            Assert.Equal(0, file.Header.PaperspaceElevation);
            Assert.Equal(new DxfPoint(0, 0, 0), file.Header.PaperspaceInsertionBase);
            Assert.Equal(new DxfPoint(-1E+20, -1E+20, -1E+20), file.Header.PaperspaceMaximumDrawingExtents);
            Assert.Equal(new DxfPoint(12, 9, 0), file.Header.PaperspaceMaximumDrawingLimits);
            Assert.Equal(new DxfPoint(1E+20, 1E+20, 1E+20), file.Header.PaperspaceMinimumDrawingExtents);
            Assert.Equal(new DxfPoint(0, 0, 0), file.Header.PaperspaceMinimumDrawingLimits);
            Assert.Equal(DxfOrthographicViewType.None, file.Header.PaperspaceOrthographicViewType);
            Assert.Equal(null, file.Header.PaperspaceOrthoUCSReference);
            Assert.Equal(null, file.Header.PaperspaceUCSDefinitionName);
            Assert.Equal(null, file.Header.PaperspaceUCSName);
            Assert.Equal(new DxfPoint(0, 0, 0), file.Header.PaperspaceUCSOrigin);
            Assert.Equal(new DxfPoint(0, 0, 0), file.Header.PaperspaceUCSOriginBack);
            Assert.Equal(new DxfPoint(0, 0, 0), file.Header.PaperspaceUCSOriginBottom);
            Assert.Equal(new DxfPoint(0, 0, 0), file.Header.PaperspaceUCSOriginFront);
            Assert.Equal(new DxfPoint(0, 0, 0), file.Header.PaperspaceUCSOriginLeft);
            Assert.Equal(new DxfPoint(0, 0, 0), file.Header.PaperspaceUCSOriginRight);
            Assert.Equal(new DxfPoint(0, 0, 0), file.Header.PaperspaceUCSOriginTop);
            Assert.Equal(new DxfVector(1, 0, 0), file.Header.PaperspaceXAxis);
            Assert.Equal(new DxfVector(0, 1, 0), file.Header.PaperspaceYAxis);
            Assert.False(file.Header.ParallelSnap);
            Assert.Equal(6, file.Header.PEditSmoothMDensith);
            Assert.Equal(6, file.Header.PEditSmoothNDensith);
            Assert.Equal(DxfPolylineCurvedAndSmoothSurfaceType.CubicBSpline, file.Header.PEditSmoothSurfaceType);
            Assert.Equal(DxfPolylineCurvedAndSmoothSurfaceType.CubicBSpline, file.Header.PEditSplineCurveType);
            Assert.Equal(70, file.Header.PercentAmbientToDiffuse);
            Assert.False(file.Header.PerpendicularSnap);
            Assert.Equal(DxfPickStyle.Group, file.Header.PickStyle);
            Assert.Equal(0, file.Header.PointDisplayMode);
            Assert.Equal(0, file.Header.PointDisplaySize);
            Assert.Equal(DxfPolySketchMode.SketchLines, file.Header.PolylineSketchMode);
            Assert.True(file.Header.PreviousReleaseTileCompatability);
            Assert.Equal(null, file.Header.ProjectName);
            Assert.True(file.Header.PromptForAttributeOnInsert);
            Assert.False(file.Header.QuadrantSnap);
            Assert.True(file.Header.RecomputeDimensionsWhileDragging);
            Assert.Equal(0, file.Header.RequiredVersions);
            Assert.True(file.Header.RetainDeletedObjects);
            Assert.True(file.Header.RetainXRefDependentVisibilitySettings);
            Assert.True(file.Header.SaveProxyGraphics);
            Assert.True(file.Header.ScaleLineTypesInPaperspace);
            Assert.Equal(null, file.Header.SecondArrowBlockName);
            Assert.Equal(0, file.Header.SecondChamferDistance);
            Assert.True(file.Header.SetUCSToWCSInDViewOrVPoint);
            Assert.Equal(DxfShadowMode.CastsAndReceivesShadows, file.Header.ShadowMode);
            Assert.Equal(0, file.Header.ShadowPlaneZOffset);
            Assert.True(file.Header.ShowAttributeEntryDialogs);
            Assert.True(file.Header.Simplify);
            Assert.Equal(0.1, file.Header.SketchRecordIncrement);
            Assert.Equal(new DxfPoint(0, 0, 0), file.Header.SnapBasePoint);
            Assert.Equal(DxfSnapIsometricPlane.Left, file.Header.SnapIsometricPlane);
            Assert.False(file.Header.SnapOn);
            Assert.Equal(0, file.Header.SnapRotationAngle);
            Assert.Equal(new DxfVector(1, 1, 0), file.Header.SnapSpacing);
            Assert.Equal(DxfSnapStyle.Standard, file.Header.SnapStyle);
            Assert.Equal(DxfSolidHistoryMode.DoesNotOverride, file.Header.SolidHistoryMode);
            Assert.True(file.Header.SortObjectsForMSlide);
            Assert.True(file.Header.SortObjectsForObjectSelection);
            Assert.True(file.Header.SortObjectsForObjectSnap);
            Assert.True(file.Header.SortObjectsForPlotting);
            Assert.True(file.Header.SortObjectsForPostScriptOutput);
            Assert.True(file.Header.SortObjectsForRedraw);
            Assert.True(file.Header.SortObjectsForRegen);
            Assert.Equal(3020, file.Header.SpacialIndexMaxDepth);
            Assert.Equal(6, file.Header.StepSizeInWalkOrFlyMode);
            Assert.Equal(2, file.Header.StepsPerSecondInWalkOrFlyMode);
            Assert.Equal(null, file.Header.Stylesheet);
            Assert.False(file.Header.SuppressFirstDimensionExtensionLine);
            Assert.False(file.Header.SuppressOutsideExtensionDimensionLines);
            Assert.False(file.Header.SuppressSecondDimensionExtensionLine);
            Assert.False(file.Header.TangentSnap);
            Assert.False(file.Header.TextAboveDimensionLine);
            Assert.Equal("STANDARD", file.Header.TextStyle);
            Assert.Equal(0, file.Header.Thickness);
            Assert.Equal(DxfTimeZone.PacificTime_US_Canada_SanFrancisco_Vancouver, file.Header.TimeZone);
            Assert.Equal(0.05, file.Header.TraceWidth);
            Assert.Equal(null, file.Header.UCSDefinitionName);
            Assert.Equal(null, file.Header.UCSName);
            Assert.Equal(new DxfPoint(0, 0, 0), file.Header.UCSOrigin);
            Assert.Equal(new DxfPoint(0, 0, 0), file.Header.UCSOriginBack);
            Assert.Equal(new DxfPoint(0, 0, 0), file.Header.UCSOriginBottom);
            Assert.Equal(new DxfPoint(0, 0, 0), file.Header.UCSOriginFront);
            Assert.Equal(new DxfPoint(0, 0, 0), file.Header.UCSOriginLeft);
            Assert.Equal(new DxfPoint(0, 0, 0), file.Header.UCSOriginRight);
            Assert.Equal(new DxfPoint(0, 0, 0), file.Header.UCSOriginTop);
            Assert.Equal(new DxfVector(1, 0, 0), file.Header.UCSXAxis);
            Assert.Equal(new DxfVector(0, 1, 0), file.Header.UCSYAxis);
            Assert.Equal(DxfUnitFormat.Decimal, file.Header.UnitFormat);
            Assert.Equal(4, file.Header.UnitPrecision);
            Assert.True(file.Header.UseACad2000SymbolTableNaming);
            Assert.False(file.Header.UseAlternateDimensioning);
            Assert.False(file.Header.UseCameraDisplay);
            Assert.True(file.Header.UseLightGlyphDisplay);
            Assert.False(file.Header.UseLimitsChecking);
            Assert.False(file.Header.UseQuickTextMode);
            Assert.True(file.Header.UseRealWorldScale);
            Assert.True(file.Header.UseRegenMode);
            Assert.Equal(0, file.Header.UserInt1);
            Assert.Equal(0, file.Header.UserInt2);
            Assert.Equal(0, file.Header.UserInt3);
            Assert.Equal(0, file.Header.UserInt4);
            Assert.Equal(0, file.Header.UserInt5);
            Assert.Equal(0, file.Header.UserReal1);
            Assert.Equal(0, file.Header.UserReal2);
            Assert.Equal(0, file.Header.UserReal3);
            Assert.Equal(0, file.Header.UserReal4);
            Assert.Equal(0, file.Header.UserReal5);
            Assert.True(file.Header.UserTimerOn);
            Assert.True(file.Header.UsesColorDependentPlotStyleTables);
            Assert.False(file.Header.UseSeparateArrowBlocksForDimensions);
            Assert.True(file.Header.UseTileModeLightSync);
            Assert.Equal(DxfAcadVersion.R12, file.Header.Version);
            Assert.Equal(new DxfPoint(0, 0, 0), file.Header.ViewCenter);
            Assert.Equal(new DxfVector(0, 0, 1), file.Header.ViewDirection);
            Assert.Equal(1, file.Header.ViewHeight);
            Assert.Equal(0, file.Header.ViewportViewScaleFactor);
        }

        [Fact]
        public void WriteHeaderWithInvalidValuesTest()
        {
            var file = new DxfFile();
            file.Header.DefaultTextHeight = -1.0; // $TEXTSIZE, normalized to 0.2
            file.Header.TraceWidth = 0.0; // $TRACEWID, normalized to 0.05
            file.Header.TextStyle = string.Empty; // $TEXTSTYLE, normalized to STANDARD
            file.Header.CurrentLayer = null; // $CLAYER, normalized to 0
            file.Header.CurrentEntityLineType = null; // $CELTYPE, normalized to BYLAYER
            file.Header.DimensionStyleName = null; // $DIMSTYLE, normalized to STANDARD
            file.Header.FileName = null; // $MENU, normalized to .

            var content = ToString(file);
            Assert.Contains(FixNewLines(@"
  9
$TEXTSIZE
 40
0.2
".Trim()), content);
            Assert.Contains(FixNewLines(@"
  9
$TRACEWID
 40
0.05
".Trim()), content);
            Assert.Contains(FixNewLines(@"
  9
$TEXTSTYLE
  7
STANDARD
".Trim()), content);
            Assert.Contains(FixNewLines(@"
  9
$CLAYER
  8
0
".Trim()), content);
            Assert.Contains(FixNewLines(@"
  9
$CELTYPE
  6
BYLAYER
".Trim()), content);
            Assert.Contains(FixNewLines(@"
  9
$DIMSTYLE
  2
STANDARD
".Trim()), content);
            Assert.Contains(FixNewLines(@"
  9
$MENU
  1
.
".Trim()), content);
        }

        #endregion

    }
}
