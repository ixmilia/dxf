IxMilia.Dxf
===========

A portable .NET library for reading and writing DXF files.  Clone and build
locally or directly consume the
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

using (FileStream fs = new FileStream(@"C:\Path\To\File.dxf", FileMode.Open))
{
    dxfFile.Save(fs);
}
```

### Status

- HEADER section - complete R10 through R2014
- CLASSES section - complete R10 through R2014
- TABLES section - complete R10 through R2014
- BLOCKS section - complete R10 through R2014
- ENTITIES section
  - common complete R10 through R2014
  - still need AcDbXrecord for ATTDEF and ATTRIB
  - entities complete R10 through R2014 _EXCEPT_
    - HATCH
    - MESH
    - MLEADER
    - MTEXT
    - SURFACE
    - TABLE
    - UNDERLAY
    - VIEWPORT
- OBJECTS section - NYI

### DXF reference

See `spec/DXF Specification.md` for links to the full Autodesk DXF specification.
