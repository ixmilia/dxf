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

- HEADER section - complete R12 through R2014
- CLASSES section - complete R12 through R2014
- TABLES section - complete R13 through R14
- BLOCKS section - complete R13 through R14
- ENTITIES section - common complete R13 through R2014, entities complete R13 through R2000, DIMENSION complete R13 through R2014
- OBJECTS section - NYI

### TODO

- 102 codes for entities
- add min/max for classes/tables/blocks/objects sections

### DXF reference

See `spec/DXF Specification.md` for links to the full Autodesk DXF specification.
