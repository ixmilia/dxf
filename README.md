IxMilia.Dxf
===========

A portable .NET library for reading and writing DXF files.

### Usage

Open a DXF file:

``` C#
using IxMilia.Dxf;
// ...
DxfFile dxfFile;
using (FileStream fs = new FileStream(@"C:\Path\To\File.dxf", FileMode.Open))
{
    dxfFile = DxfFile.Load(fs);
}
```

Save a DXF file:

``` C#
using (FileStream fs = new FileStream(@"C:\Path\To\File.dxf", FileMode.Open))
{
    dxfFile.Save(fs);
}
```

### Status

- HEADER section - complete through R2014
- CLASSES section - complete through R2014
- TABLES section - complete through R14
- BLOCKS section - complete through R14
- ENTITIES section - common complete through R2014, entities complete through R2000, DIMENSION complete through R2014
- OBJECTS section - NYI

### TODO

- 102 codes for entities
- add min/max for classes/tables/blocks/objects sections

### DXF reference

http://usa.autodesk.com/adsk/servlet/item?siteID=123112&id=12272454&linkID=10809853 (use archive.org, May 9, 2013)
http://www.autodesk.com/techpubs/autocad/acad2000/dxf/
