IxMilia.Dxf.ReferenceCollector
==============================

Download the AutoCAD DXF reference as a single, self-contained HTML file.

Usage:

``` bash
dotnet run -- [dxf-version]
```

Valid values for `[dxf-version]` are:

* 2015
* 2016
* 2017
* 2018
* 2019
* 2020

The result is placed at `.\dxf-reference-R[dxf-version].html`, e.g., `.\dxf-reference-R2020.html`.
