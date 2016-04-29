IxMilia.Dxf
===========

A portable .NET library for reading and writing DXF and DXB files.  Clone and
build locally or directly consume the
[NuGet package](http://www.nuget.org/packages/IxMilia.Dxf/).

### Usage

Open a DXF file:

``` C#
using System.IO;
using IxMilia.Dxf;
using IxMilia.Dxf.Entities;
// ...

DxfFile dxfFile;
using (FileStream fs = new FileStream(@"C:\Path\To\File.dxf", FileMode.Open))
{
    dxfFile = DxfFile.Load(fs);
}

foreach (DxfEntity entity in dxfFile.Entities)
{
    switch (entity.EntityType)
    {
        case DxfEntityType.Line:
            DxfLine line = (DxfLine)entity;
            // ...
            break;
        // ...
    }
}
```

Save a DXF file:

``` C#
using System.IO;
using IxMilia.Dxf;
using IxMilia.Dxf.Entities;
// ...

DxfFile dxfFile = new DxfFile();
dxfFile.Entities.Add(new DxfLine(new DxfPoint(0, 0, 0), new DxfPoint(50, 50, 0)));
// ...

using (FileStream fs = new FileStream(@"C:\Path\To\File.dxf", FileMode.Create))
{
    dxfFile.Save(fs);
}
```

### Status

Support for DXF files is complete from versions R10 through R2014 _EXCEPT_ for the following entities:
- HATCH
- MESH
- MLEADER
- SURFACE
- TABLE
- VIEWPORT

### DXF Reference

Since I don't want to fall afoul of Autodesk's lawyers, this repo can't include the actual DXF documentation.  It can, however contain links to the official documents that I've been able to scrape together.  For most scenarios the 2014 documentation should suffice, but all other versions are included here for backwards compatibility and reference between versions.

[R10 (non-Autodesk source)](http://www.martinreddy.net/gfx/3d/DXF10.spec)

[R11 (differences between R10 and R11)](http://autodesk.blogs.com/between_the_lines/ACAD_R11.html)

[R12 (non-Autodesk source)](http://www.martinreddy.net/gfx/3d/DXF12.spec)

[R13 (self-extracting 16-bit executable)](http://www.autodesk.com/techpubs/autocad/dxf/dxf13_hlp.exe)

[R14](http://www.autodesk.com/techpubs/autocad/acadr14/dxf/index.htm)

[2000](http://www.autodesk.com/techpubs/autocad/acad2000/dxf/index.htm)

[2002](http://www.autodesk.com/techpubs/autocad/dxf/dxf2002.pdf)

[2004](http://download.autodesk.com/prodsupp/downloads/dxf.pdf)

[2005](http://download.autodesk.com/prodsupp/downloads/acad_dxf.pdf)

[2006](http://images.autodesk.com/adsk/files/dxf_format.pdf)

2007 (Autodesk's link erroneously points to the R2008 documentation)

[2008](http://images.autodesk.com/adsk/files/acad_dxf0.pdf)

[2009](http://images.autodesk.com/adsk/files/acad_dxf.pdf)

[2010](http://images.autodesk.com/adsk/files/acad_dxf1.pdf)

[2011](http://images.autodesk.com/adsk/files/acad_dxf2.pdf)

[2012](http://images.autodesk.com/adsk/files/autocad_2012_pdf_dxf-reference_enu.pdf)

[2013](http://images.autodesk.com/adsk/files/autocad_2013_pdf_dxf_reference_enu.pdf)

[2014](http://images.autodesk.com/adsk/files/autocad_2014_pdf_dxf_reference_enu.pdf)

These links were compiled from the archive.org May 9, 2013 snapshot of http://usa.autodesk.com/adsk/servlet/item?siteID=123112&id=12272454&linkID=10809853
(https://web.archive.org/web/20130509144333/http://usa.autodesk.com/adsk/servlet/item?siteID=123112&id=12272454&linkID=10809853)
