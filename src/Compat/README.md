The files in this directory are meant to represent the current state of output and library compatability and should be useful for diffing.  The file contents are as follows:

- `bare.dxf` - The smallest possible DXF file with a single line that any library/viewer should be able to open.
- `i.rXXXX.dxf` - The result of creating a DXF file with a single line from (0, 0) to (10, 10), setting the `Header.Version` property to `XXXX`, then saving the file.
- `t.rXXXX.dxf` - The result of running `bare.dxf` through the `TeighaFileConverter.exe` tool with the specified output version of `XXXX`.
