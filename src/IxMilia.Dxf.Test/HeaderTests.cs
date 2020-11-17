using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using IxMilia.Dxf.Entities;
using IxMilia.Dxf.Sections;
using Xunit;

namespace IxMilia.Dxf.Test
{
    public class HeaderTests : AbstractDxfTests
    {
        [Fact]
        public void SpecificHeaderValuesTest()
        {
            var file = Section("HEADER",
                (9, "$ACADMAINTVER"), (70, 16),
                (9, "$ACADVER"), (1, "AC1012"),
                (9, "$ANGBASE"), (50, 55.0),
                (9, "$ANGDIR"), (70, 1),
                (9, "$ATTMODE"), (70, 1),
                (9, "$AUNITS"), (70, 3),
                (9, "$AUPREC"), (70, 7),
                (9, "$CLAYER"), (8, "<current layer>"),
                (9, "$LUNITS"), (70, 6),
                (9, "$LUPREC"), (70, 7)
            );
            Assert.Equal(16, file.Header.MaintenanceVersion);
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
            Assert.True(DxfTextReader.TryParseDoubleValue("2451544.91568287", out var dateAsDouble));
            var date = DxfCommonConverters.DateDouble(dateAsDouble);
            Assert.Equal(new DateTime(1999, 12, 31, 21, 58, 35), date);

            // verify writing
            dateAsDouble = DxfCommonConverters.DateDouble(date);
            var dateAsString = DxfWriter.DoubleAsString(dateAsDouble);
            Assert.Equal("2451544.91568287", dateAsString);
        }

        [Fact]
        public void ReadLayerTableTest()
        {
            var file = Section("TABLES",
                (0, "TABLE"),
                (2, "LAYER"),
                (0, "LAYER"),
                (2, "a"),
                (62, 12),
                (0, "LAYER"),
                (102, "{APP_NAME"),
                (1, "foo"),
                (2, "bar"),
                (102, "}"),
                (2, "b"),
                (62, 13),
                (0, "ENDTAB")
            );
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
            VerifyFileContains(file,
                (0, "LAYER"),
                (5, "#"),
                (102, "{APP_NAME"),
                (1, "foo"),
                (2, "bar"),
                (102, "}")
            );
        }

        [Fact]
        public void ViewPortTableTest()
        {
            var file = Section("TABLES",
                (0, "TABLE"),
                (2, "VPORT"),
                    (0, "VPORT"), // empty vport
                    (0, "VPORT"), // vport with values
                    (2, "vport-2"), // name
                    (10, 11.0), // lower left
                    (20, 22.0),
                    (11, 33.0), // upper right
                    (21, 44.0),
                    (12, 55.0), // view center
                    (22, 66.0),
                    (13, 77.0), // snap base
                    (23, 88.0),
                    (14, 99.0), // snap spacing
                    (24, 12.0),
                    (15, 13.0), // grid spacing
                    (25, 14.0),
                    (16, 15.0), // view direction
                    (26, 16.0),
                    (36, 17.0),
                    (17, 18.0), // target view point
                    (27, 19.0),
                    (37, 20.0),
                    (40, 21.0), // view height
                    (41, 22.0), // view port aspect ratio
                    (42, 23.0), // lens length
                    (43, 24.0), // front clipping plane
                    (44, 25.0), // back clipping plane
                    (50, 26.0), // snap rotation angle
                    (51, 27.0), // view twist angle
                (0, "ENDTAB")
            );
            var viewPorts = file.ViewPorts;
            Assert.Equal(2, viewPorts.Count);

            // defaults
            Assert.Null(viewPorts[0].Name);
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
            var file = Section("HEADER",
                (9, "$ACADVER"), (1, "15.0S")
            );
            Assert.Equal(DxfAcadVersion.R2000, file.Header.Version);
            Assert.True(file.Header.IsRestrictedVersion);
        }

        [Fact]
        public void ReadEmptyFingerPrintGuidTest()
        {
            var file = Section("HEADER",
                (9, "$FINGERPRINTGUID"), (2, ""),
                (9, "$ACADVER"), (1, "AC1012")
            );
            Assert.Equal(Guid.Empty, file.Header.FingerprintGuid);
        }

        [Fact]
        public void ReadAlternateMaintenenceVersionTest()
        {
            // traditional short value
            var file = Section("HEADER",
                (9, "$ACADMAINTVER"), (70, 42)
            );
            Assert.Equal(42, file.Header.MaintenanceVersion);

            // alternate long value
            file = Section("HEADER",
                (9, "$ACADMAINTVER"), (90, 42)
            );
            Assert.Equal(42, file.Header.MaintenanceVersion);
        }

        [Fact]
        public void WriteAppropriateMaintenenceVersionTest()
        {
            var file = new DxfFile();
            file.Header.MaintenanceVersion = 42;

            // < R2018 writes code 70
            file.Header.Version = DxfAcadVersion.R2013;
            VerifyFileContains(file,
                DxfSectionType.Header,
                (9, "$ACADMAINTVER"),
                (70, 42)
            );

            // >= R2018 writes code 90
            file.Header.Version = DxfAcadVersion.R2018;
            VerifyFileContains(file,
                DxfSectionType.Header,
                (9, "$ACADMAINTVER"),
                (90, 42)
            );
        }

        [Fact]
        public void WriteDefaultHeaderValuesTest()
        {
            VerifyFileContains(new DxfFile(),
                DxfSectionType.Header,
                (9, "$DIMGAP"), (40, 0.09)
            );
        }

        [Fact]
        public void WriteSpecificHeaderValuesTest()
        {
            var file = new DxfFile();
            file.Header.DimensionLineGap = 11.0;
            VerifyFileContains(file,
                DxfSectionType.Header,
                (9, "$DIMGAP"), (40, 11.0)
            );
        }

        [Fact]
        public void WriteLayersTest()
        {
            var file = new DxfFile();
            file.Layers.Add(new DxfLayer("default"));
            VerifyFileContains(file,
                DxfSectionType.Tables,
                (0, "LAYER"),
                (5, "#"),
                (100, "AcDbSymbolTableRecord"),
                (2, "default"),
                (70, 0),
                (62, 7)
            );
        }

        [Fact]
        public void WriteViewportTest()
        {
            var file = new DxfFile();
            file.ViewPorts.Add(new DxfViewPort());
            VerifyFileContains(file,
                DxfSectionType.Tables,
                (0, "VPORT"),
                (5, "#"),
                (100, "AcDbSymbolTableRecord"),
                (2, ""),
                (70, 0),
                (10, 0.0),
                (20, 0.0),
                (11, 1.0),
                (21, 1.0),
                (12, 0.0),
                (22, 0.0),
                (13, 0.0),
                (23, 0.0),
                (14, 1.0),
                (24, 1.0),
                (15, 1.0),
                (25, 1.0),
                (16, 0.0),
                (26, 0.0),
                (36, 1.0),
                (17, 0.0),
                (27, 0.0),
                (37, 0.0),
                (40, 1.0),
                (41, 1.0),
                (42, 50.0),
                (43, 0.0),
                (44, 0.0),
                (50, 0.0),
                (51, 0.0),
                (71, 0),
                (72, 1000),
                (73, 1),
                (74, 3),
                (75, 0),
                (76, 0),
                (77, 0),
                (78, 0)
            );
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
            VerifyFileContains(file,
                (9, "$ACADVER"), (1, "AC1015S")
            );
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
            var header = new DxfHeader();

            // ensure they default to 0
            Assert.Equal(0u, header.CurrentMaterialHandle);
            Assert.Equal(0u, header.InterferenceObjectVisualStylePointer);
            Assert.Equal(0u, header.InterferenceViewPortVisualStylePointer);

            var pairs = new List<DxfCodePair>();
            header.AddValueToList(pairs);
            Assert.DoesNotContain(pairs, p => p.Code == 9 && p.StringValue == "$CMATERIAL");
            Assert.DoesNotContain(pairs, p => p.Code == 9 && p.StringValue == "$INTERFEREOBJVS");
            Assert.DoesNotContain(pairs, p => p.Code == 9 && p.StringValue == "$INTERFEREVPVS");
        }

        private static void TestHeaderOrder(string expectedOrderText, DxfAcadVersion version)
        {
            var header = new DxfHeader();
            header.Version = version;
            header.CurrentMaterialHandle = 100u;
            header.InterferenceObjectVisualStylePointer = 101u;
            header.InterferenceViewPortVisualStylePointer = 102u;
            var expectedOrder = expectedOrderText
                .Split("\n".ToCharArray(), StringSplitOptions.RemoveEmptyEntries)
                .Where(s => s.StartsWith("$"))
                .Select(s => s.Trim())
                .ToArray();
            var pairs = new List<DxfCodePair>();
            header.AddValueToList(pairs);
            var actualOrder = pairs.Where(p => p.Code == 9).Select(p => p.StringValue).ToArray();
            AssertArrayEqual(expectedOrder, actualOrder);
        }

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
            var header = new DxfHeader();
            Assert.True(header.AlignDirection);
            Assert.Equal(2, header.AlternateDimensioningDecimalPlaces);
            Assert.Equal(25.4, header.AlternateDimensioningScaleFactor);
            Assert.Null(header.AlternateDimensioningSuffix);
            Assert.Equal(2, header.AlternateDimensioningToleranceDecimalPlaces);
            Assert.Equal(DxfUnitZeroSuppression.SuppressZeroFeetAndZeroInches, header.AlternateDimensioningToleranceZeroSupression);
            Assert.Equal(0, header.AlternateDimensioningUnitRounding);
            Assert.Equal(DxfUnitFormat.Decimal, header.AlternateDimensioningUnits);
            Assert.Equal(DxfUnitZeroSuppression.SuppressZeroFeetAndZeroInches, header.AlternateDimensioningZeroSupression);
            Assert.Equal(0, header.AngleBetweenYAxisAndNorth);
            Assert.Equal(DxfAngleDirection.CounterClockwise, header.AngleDirection);
            Assert.Equal(DxfAngleFormat.DecimalDegrees, header.AngleUnitFormat);
            Assert.Equal(0, header.AngleUnitPrecision);
            Assert.Equal(0, header.AngleZeroDirection);
            Assert.Equal(0, header.AngularDimensionPrecision);
            Assert.False(header.ApparentIntersectionSnap);
            Assert.Null(header.ArrowBlockName);
            Assert.Equal(DxfAttributeVisibility.Normal, header.AttributeVisibility);
            Assert.False(header.AxisOn);
            Assert.Equal(new DxfVector(0, 0, 0), header.AxisTickSpacing);
            Assert.False(header.BlipMode);
            Assert.Equal(0, header.CameraHeight);
            Assert.True(header.CanUseInPlaceReferenceEditing);
            Assert.Equal(0.09, header.CenterMarkSize);
            Assert.True(header.CenterSnap);
            Assert.Equal(0, header.ChamferAngle);
            Assert.Equal(0, header.ChamferLength);
            Assert.False(header.Close);
            Assert.Equal(DxfCoordinateDisplay.ContinuousUpdate, header.CoordinateDisplay);
            Assert.True(header.CreateAssociativeDimensioning);
            Assert.Equal(DxfColor.ByLayer, header.CurrentEntityColor);
            Assert.Equal("BYLAYER", header.CurrentEntityLineType);
            Assert.Equal(1, header.CurrentEntityLineTypeScale);
            Assert.Equal("0", header.CurrentLayer);
            Assert.Equal(0u, header.CurrentMaterialHandle);
            Assert.Equal(DxfJustification.Top, header.CurrentMultilineJustification);
            Assert.Equal(1, header.CurrentMultilineScale);
            Assert.Equal("STANDARD", header.CurrentMultilineStyle);
            Assert.Equal(DxfUnits.Unitless, header.DefaultDrawingUnits);
            Assert.Equal(0, header.DefaultPolylineWidth);
            Assert.Equal(0.2, header.DefaultTextHeight);
            Assert.Equal(DxfUnderlayFrameMode.None, header.DgnUnderlayFrameMode);
            Assert.Equal(DxfUnitZeroSuppression.SuppressZeroFeetAndZeroInches, header.DimensionAngleZeroSuppression);
            Assert.Equal(DxfDimensionArcSymbolDisplayMode.SymbolBeforeText, header.DimensionArcSymbolDisplayMode);
            Assert.False(header.DimensionCursorControlsTextPosition);
            Assert.Equal('.', header.DimensionDecimalSeparatorChar);
            Assert.Equal(0, header.DimensionDistanceRoundingValue);
            Assert.Equal(DxfColor.ByBlock, header.DimensionExtensionLineColor);
            Assert.Equal(0.18, header.DimensionExtensionLineExtension);
            Assert.Equal(0.0625, header.DimensionExtensionLineOffset);
            Assert.Equal(DxfLineWeight.ByLayer, header.DimensionExtensionLineWeight);
            Assert.Null(header.DimensionFirstExtensionLineType);
            Assert.Equal(DxfAngleFormat.DecimalDegrees, header.DimensioningAngleFormat);
            Assert.Equal(0.18, header.DimensioningArrowSize);
            Assert.Equal(1, header.DimensioningScaleFactor);
            Assert.Null(header.DimensioningSuffix);
            Assert.Equal(0.18, header.DimensioningTextHeight);
            Assert.Equal(0, header.DimensioningTickSize);
            Assert.Null(header.DimensionLeaderBlockName);
            Assert.Equal(1, header.DimensionLinearMeasurementsScaleFactor);
            Assert.Equal(DxfColor.ByBlock, header.DimensionLineColor);
            Assert.Equal(0, header.DimensionLineExtension);
            Assert.Equal(1, header.DimensionLineFixedLength);
            Assert.False(header.DimensionLineFixedLengthOn);
            Assert.Equal(0.09, header.DimensionLineGap);
            Assert.Equal(0.38, header.DimensionLineIncrement);
            Assert.Null(header.DimensionLineType);
            Assert.Equal(DxfLineWeight.ByLayer, header.DimensionLineWeight);
            Assert.Equal(0, header.DimensionMinusTolerance);
            Assert.Equal(DxfNonAngularUnits.Decimal, header.DimensionNonAngularUnits);
            Assert.Equal(DxfDimensionAssociativity.NonAssociativeObjects, header.DimensionObjectAssociativity);
            Assert.Equal(0, header.DimensionPlusTolerance);
            Assert.Null(header.DimensionSecondExtensionLineType);
            Assert.Equal("STANDARD", header.DimensionStyleName);
            Assert.Equal(DxfDimensionFit.TextAndArrowsOutsideLines, header.DimensionTextAndArrowPlacement);
            Assert.Equal(DxfDimensionTextBackgroundColorMode.None, header.DimensionTextBackgroundColorMode);
            Assert.Equal(DxfColor.ByBlock, header.DimensionTextColor);
            Assert.Equal(DxfTextDirection.LeftToRight, header.DimensionTextDirection);
            Assert.Equal(DxfDimensionFractionFormat.HorizontalStacking, header.DimensionTextHeightScaleFactor);
            Assert.True(header.DimensionTextInsideHorizontal);
            Assert.Equal(DxfDimensionTextJustification.AboveLineCenter, header.DimensionTextJustification);
            Assert.Equal(DxfDimensionTextMovementRule.MoveLineWithText, header.DimensionTextMovementRule);
            Assert.True(header.DimensionTextOutsideHorizontal);
            Assert.Equal("STANDARD", header.DimensionTextStyle);
            Assert.Equal(4, header.DimensionToleranceDecimalPlaces);
            Assert.Equal(1, header.DimensionToleranceDisplayScaleFactor);
            Assert.Equal(DxfJustification.Middle, header.DimensionToleranceVerticalJustification);
            Assert.Equal(DxfUnitZeroSuppression.SuppressZeroFeetAndZeroInches, header.DimensionToleranceZeroSuppression);
            Assert.Equal(Math.PI / 4.0, header.DimensionTransverseSegmentAngleInJoggedRadius);
            Assert.Equal(DxfUnitFormat.Decimal, header.DimensionUnitFormat);
            Assert.Equal(4, header.DimensionUnitToleranceDecimalPlaces);
            Assert.Equal(DxfUnitZeroSuppression.SuppressZeroFeetAndZeroInches, header.DimensionUnitZeroSuppression);
            Assert.Equal(0, header.DimensionVerticalTextPosition);
            Assert.False(header.DisplayFractionsInInput);
            Assert.False(header.DisplayIntersectionPolylines);
            Assert.False(header.DisplayLineweightInModelAndLayoutTab);
            Assert.False(header.DisplaySilhouetteCurvesInWireframeMode);
            Assert.False(header.DisplaySplinePolygonControl);
            Assert.Equal(DxfDragMode.Auto, header.DragMode);
            Assert.Equal("ANSI_1252", header.DrawingCodePage);
            Assert.Equal(DxfDrawingUnits.English, header.DrawingUnits);
            Assert.False(header.DrawOrthoganalLines);
            Assert.Equal(Dxf3DDwfPrecision.Deviation_0_5, header.Dwf3DPrecision);
            Assert.Equal(DxfUnderlayFrameMode.DisplayNoPlot, header.DwfUnderlayFrameMode);
            Assert.Equal(DxfColor.ByBlock, header.DxfDimensionTextBackgroundCustomColor);
            Assert.Equal(DxfShadeEdgeMode.FacesInEntityColorEdgesInBlack, header.EdgeShading);
            Assert.Equal(0, header.Elevation);
            Assert.Equal(DxfEndCapSetting.None, header.EndCapSetting);
            Assert.True(header.EndPointSnap);
            Assert.False(header.ExtensionSnap);
            Assert.True(header.FastZoom);
            Assert.Equal(".", header.FileName);
            Assert.Equal(0, header.FilletRadius);
            Assert.True(header.FillModeOn);
            Assert.Null(header.FirstArrowBlockName);
            Assert.Equal(0, header.FirstChamferDistance);
            Assert.False(header.ForceDimensionLineExtensionsOutsideIfTextIs);
            Assert.False(header.ForceDimensionTextInsideExtensions);
            Assert.False(header.GenerateDimensionLimits);
            Assert.False(header.GenerateDimensionTolerances);
            Assert.False(header.GridOn);
            Assert.Equal(new DxfVector(1, 1, 0), header.GridSpacing);
            Assert.Equal(0, header.HaloGapPercent);
            Assert.True(header.HandlesEnabled);
            Assert.False(header.HideTextObjectsWhenProducingHiddenView);
            Assert.Null(header.HyperlinkBase);
            Assert.Equal(new DxfPoint(0, 0, 0), header.InsertionBase);
            Assert.False(header.InsertionSnap);
            Assert.Equal(1, header.InterferenceObjectColor.Index);
            Assert.Equal(0u, header.InterferenceObjectVisualStylePointer);
            Assert.Equal(0u, header.InterferenceViewPortVisualStylePointer);
            Assert.Equal(DxfColor.ByEntity, header.IntersectionPolylineColor);
            Assert.True(header.IntersectionSnap);
            Assert.False(header.IsPolylineContinuousAroundVertices);
            Assert.False(header.IsRestrictedVersion);
            Assert.True(header.IsViewportScaledToFit);
            Assert.Equal(DxfXrefClippingBoundaryVisibility.DisplayedNotPlotted, header.IsXRefClippingBoundaryVisible);
            Assert.Equal(4, header.LastPolySolidHeight);
            Assert.Equal(0.25, header.LastPolySolidWidth);
            Assert.Null(header.LastSavedBy);
            Assert.Equal(37.795, header.Latitude);
            Assert.Equal(DxfLayerAndSpatialIndexSaveMode.None, header.LayerAndSpatialIndexSaveMode);
            Assert.Equal(50, header.LensLength);
            Assert.False(header.LimitCheckingInPaperspace);
            Assert.Equal(8, header.LineSegmentsPerSplinePatch);
            Assert.Equal(1, header.LineTypeScale);
            Assert.Equal(DxfJoinStyle.None, header.LineweightJointSetting);
            Assert.Equal(DxfLoftedObjectNormalMode.SmoothFit, header.LoftedObjectNormalMode);
            Assert.Equal(7, header.LoftFlags);
            Assert.Equal(Math.PI / 2.0, header.LoftOperationFirstDraftAngle);
            Assert.Equal(0, header.LoftOperationFirstMagnitude);
            Assert.Equal(Math.PI / 2.0, header.LoftOperationSecondDraftAngle);
            Assert.Equal(0, header.LoftOperationSecondMagnitude);
            Assert.Equal(-122.394, header.Longitude);
            Assert.Equal(0, header.MaintenanceVersion);
            Assert.Equal(64, header.MaximumActiveViewports);
            Assert.Equal(new DxfPoint(0, 0, 0), header.MaximumDrawingExtents);
            Assert.Equal(new DxfPoint(12, 9, 0), header.MaximumDrawingLimits);
            Assert.Equal(6, header.MeshTabulationsInFirstDirection);
            Assert.Equal(6, header.MeshTabulationsInSecondDirection);
            Assert.False(header.MidPointSnap);
            Assert.Equal(new DxfPoint(0, 0, 0), header.MinimumDrawingExtents);
            Assert.Equal(new DxfPoint(0, 0, 0), header.MinimumDrawingLimits);
            Assert.False(header.MirrorText);
            Assert.False(header.NearestSnap);
            Assert.Equal(DxfLineWeight.ByBlock, header.NewObjectLineWeight);
            Assert.Equal(DxfPlotStyle.ByLayer, header.NewObjectPlotStyle);
            Assert.False(header.NewSolidsContainHistory);
            Assert.Equal(0u, header.NextAvailableHandle);
            Assert.False(header.NodeSnap);
            Assert.True(header.NoTwist);
            Assert.Equal(37, header.ObjectSnapFlags);
            Assert.Equal(127, header.ObjectSortingMethodsFlags);
            Assert.Equal(DxfColor.ByEntity, header.ObscuredLineColor);
            Assert.Equal(DxfLineTypeStyle.Off, header.ObscuredLineTypeStyle);
            Assert.False(header.OleStartup);
            Assert.Equal(DxfOrthographicViewType.None, header.OrthographicViewType);
            Assert.Null(header.OrthoUCSReference);
            Assert.Equal(0, header.PaperspaceElevation);
            Assert.Equal(new DxfPoint(0, 0, 0), header.PaperspaceInsertionBase);
            Assert.Equal(new DxfPoint(-1E+20, -1E+20, -1E+20), header.PaperspaceMaximumDrawingExtents);
            Assert.Equal(new DxfPoint(12, 9, 0), header.PaperspaceMaximumDrawingLimits);
            Assert.Equal(new DxfPoint(1E+20, 1E+20, 1E+20), header.PaperspaceMinimumDrawingExtents);
            Assert.Equal(new DxfPoint(0, 0, 0), header.PaperspaceMinimumDrawingLimits);
            Assert.Equal(DxfOrthographicViewType.None, header.PaperspaceOrthographicViewType);
            Assert.Null(header.PaperspaceOrthoUCSReference);
            Assert.Null(header.PaperspaceUCSDefinitionName);
            Assert.Null(header.PaperspaceUCSName);
            Assert.Equal(new DxfPoint(0, 0, 0), header.PaperspaceUCSOrigin);
            Assert.Equal(new DxfPoint(0, 0, 0), header.PaperspaceUCSOriginBack);
            Assert.Equal(new DxfPoint(0, 0, 0), header.PaperspaceUCSOriginBottom);
            Assert.Equal(new DxfPoint(0, 0, 0), header.PaperspaceUCSOriginFront);
            Assert.Equal(new DxfPoint(0, 0, 0), header.PaperspaceUCSOriginLeft);
            Assert.Equal(new DxfPoint(0, 0, 0), header.PaperspaceUCSOriginRight);
            Assert.Equal(new DxfPoint(0, 0, 0), header.PaperspaceUCSOriginTop);
            Assert.Equal(new DxfVector(1, 0, 0), header.PaperspaceXAxis);
            Assert.Equal(new DxfVector(0, 1, 0), header.PaperspaceYAxis);
            Assert.False(header.ParallelSnap);
            Assert.Equal(6, header.PEditSmoothMDensity);
            Assert.Equal(6, header.PEditSmoothNDensity);
            Assert.Equal(DxfPolylineCurvedAndSmoothSurfaceType.CubicBSpline, header.PEditSmoothSurfaceType);
            Assert.Equal(DxfPolylineCurvedAndSmoothSurfaceType.CubicBSpline, header.PEditSplineCurveType);
            Assert.Equal(70, header.PercentAmbientToDiffuse);
            Assert.False(header.PerpendicularSnap);
            Assert.Equal(DxfPickStyle.Group, header.PickStyle);
            Assert.Equal(0, header.PointDisplayMode);
            Assert.Equal(0, header.PointDisplaySize);
            Assert.Equal(DxfPolySketchMode.SketchLines, header.PolylineSketchMode);
            Assert.True(header.PreviousReleaseTileCompatibility);
            Assert.Null(header.ProjectName);
            Assert.True(header.PromptForAttributeOnInsert);
            Assert.False(header.QuadrantSnap);
            Assert.True(header.RecomputeDimensionsWhileDragging);
            Assert.Equal(0, header.RequiredVersions);
            Assert.True(header.RetainDeletedObjects);
            Assert.True(header.RetainXRefDependentVisibilitySettings);
            Assert.True(header.SaveProxyGraphics);
            Assert.True(header.ScaleLineTypesInPaperspace);
            Assert.Null(header.SecondArrowBlockName);
            Assert.Equal(0, header.SecondChamferDistance);
            Assert.True(header.SetUCSToWCSInDViewOrVPoint);
            Assert.Equal(DxfShadowMode.CastsAndReceivesShadows, header.ShadowMode);
            Assert.Equal(0, header.ShadowPlaneZOffset);
            Assert.True(header.ShowAttributeEntryDialogs);
            Assert.True(header.Simplify);
            Assert.Equal(0.1, header.SketchRecordIncrement);
            Assert.Equal(new DxfPoint(0, 0, 0), header.SnapBasePoint);
            Assert.Equal(DxfSnapIsometricPlane.Left, header.SnapIsometricPlane);
            Assert.False(header.SnapOn);
            Assert.Equal(0, header.SnapRotationAngle);
            Assert.Equal(new DxfVector(1, 1, 0), header.SnapSpacing);
            Assert.Equal(DxfSnapStyle.Standard, header.SnapStyle);
            Assert.Equal(DxfSolidHistoryMode.DoesNotOverride, header.SolidHistoryMode);
            Assert.True(header.SortObjectsForMSlide);
            Assert.True(header.SortObjectsForObjectSelection);
            Assert.True(header.SortObjectsForObjectSnap);
            Assert.True(header.SortObjectsForPlotting);
            Assert.True(header.SortObjectsForPostScriptOutput);
            Assert.True(header.SortObjectsForRedraw);
            Assert.True(header.SortObjectsForRegen);
            Assert.Equal(3020, header.SpacialIndexMaxDepth);
            Assert.Equal(6, header.StepSizeInWalkOrFlyMode);
            Assert.Equal(2, header.StepsPerSecondInWalkOrFlyMode);
            Assert.Null(header.Stylesheet);
            Assert.False(header.SuppressFirstDimensionExtensionLine);
            Assert.False(header.SuppressOutsideExtensionDimensionLines);
            Assert.False(header.SuppressSecondDimensionExtensionLine);
            Assert.False(header.TangentSnap);
            Assert.False(header.TextAboveDimensionLine);
            Assert.Equal("STANDARD", header.TextStyle);
            Assert.Equal(0, header.Thickness);
            Assert.Equal(DxfTimeZone.PacificTime_US_Canada_SanFrancisco_Vancouver, header.TimeZone);
            Assert.Equal(0.05, header.TraceWidth);
            Assert.Null(header.UCSDefinitionName);
            Assert.Null(header.UCSName);
            Assert.Equal(new DxfPoint(0, 0, 0), header.UCSOrigin);
            Assert.Equal(new DxfPoint(0, 0, 0), header.UCSOriginBack);
            Assert.Equal(new DxfPoint(0, 0, 0), header.UCSOriginBottom);
            Assert.Equal(new DxfPoint(0, 0, 0), header.UCSOriginFront);
            Assert.Equal(new DxfPoint(0, 0, 0), header.UCSOriginLeft);
            Assert.Equal(new DxfPoint(0, 0, 0), header.UCSOriginRight);
            Assert.Equal(new DxfPoint(0, 0, 0), header.UCSOriginTop);
            Assert.Equal(new DxfVector(1, 0, 0), header.UCSXAxis);
            Assert.Equal(new DxfVector(0, 1, 0), header.UCSYAxis);
            Assert.Equal(DxfUnitFormat.Decimal, header.UnitFormat);
            Assert.Equal(4, header.UnitPrecision);
            Assert.True(header.UseACad2000SymbolTableNaming);
            Assert.False(header.UseAlternateDimensioning);
            Assert.False(header.UseCameraDisplay);
            Assert.True(header.UseLightGlyphDisplay);
            Assert.False(header.UseLimitsChecking);
            Assert.False(header.UseQuickTextMode);
            Assert.True(header.UseRealWorldScale);
            Assert.True(header.UseRegenMode);
            Assert.Equal(0, header.UserInt1);
            Assert.Equal(0, header.UserInt2);
            Assert.Equal(0, header.UserInt3);
            Assert.Equal(0, header.UserInt4);
            Assert.Equal(0, header.UserInt5);
            Assert.Equal(0, header.UserReal1);
            Assert.Equal(0, header.UserReal2);
            Assert.Equal(0, header.UserReal3);
            Assert.Equal(0, header.UserReal4);
            Assert.Equal(0, header.UserReal5);
            Assert.True(header.UserTimerOn);
            Assert.True(header.UsesColorDependentPlotStyleTables);
            Assert.False(header.UseSeparateArrowBlocksForDimensions);
            Assert.True(header.UseTileModeLightSync);
            Assert.Equal(DxfAcadVersion.R12, header.Version);
            Assert.Equal(new DxfPoint(0, 0, 0), header.ViewCenter);
            Assert.Equal(new DxfVector(0, 0, 1), header.ViewDirection);
            Assert.Equal(1, header.ViewHeight);
            Assert.Equal(0, header.ViewportViewScaleFactor);
        }

        [Fact]
        public void WriteHeaderWithInvalidValuesTest()
        {
            var header = new DxfHeader();
            header.DefaultTextHeight = -1.0; // $TEXTSIZE, normalized to 0.2
            header.TraceWidth = 0.0; // $TRACEWID, normalized to 0.05
            header.TextStyle = string.Empty; // $TEXTSTYLE, normalized to STANDARD
            header.CurrentLayer = null; // $CLAYER, normalized to 0
            header.CurrentEntityLineType = null; // $CELTYPE, normalized to BYLAYER
            header.DimensionStyleName = null; // $DIMSTYLE, normalized to STANDARD
            header.FileName = null; // $MENU, normalized to .

            var pairs = new List<DxfCodePair>();
            header.AddValueToList(pairs);

            AssertContains(pairs,
                (9, "$TEXTSIZE"), (40, 0.2)
            );
            AssertContains(pairs,
                (9, "$TRACEWID"), (40, 0.05)
            );
            AssertContains(pairs,
                (9, "$TEXTSTYLE"), (7, "STANDARD")
            );
            AssertContains(pairs,
                (9, "$CLAYER"), (8, "0")
            );
            AssertContains(pairs,
                (9, "$CELTYPE"), (6, "BYLAYER")
            );
            AssertContains(pairs,
                (9, "$DIMSTYLE"), (2, "STANDARD")
            );
            AssertContains(pairs,
                (9, "$MENU"), (1, ".")
            );
        }
    }
}
