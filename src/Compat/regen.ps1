# Copyright (c) IxMilia.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

$dllPath = [System.IO.Path]::GetFullPath((Join-Path (pwd) "..\Binaries\Debug\IxMilia.Dxf.dll"))
Add-Type -Path $dllPath
$file = New-Object -TypeName IxMilia.Dxf.DxfFile
$line = New-Object -TypeName IxMilia.Dxf.Entities.DxfLine
$line.P1 = New-Object -TypeName IxMilia.Dxf.DxfPoint(0, 0, 0)
$line.P2 = New-Object -TypeName IxMilia.Dxf.DxfPoint(10, 10, 0)
$file.Entities.Add($line)

$versions =
    [IxMilia.Dxf.DxfAcadVersion]::R9,
    [IxMilia.Dxf.DxfAcadVersion]::R10,
    [IxMilia.Dxf.DxfAcadVersion]::R11,
    [IxMilia.Dxf.DxfAcadVersion]::R12,
    [IxMilia.Dxf.DxfAcadVersion]::R13,
    [IxMilia.Dxf.DxfAcadVersion]::R14,
    [IxMilia.Dxf.DxfAcadVersion]::R2000,
    [IxMilia.Dxf.DxfAcadVersion]::R2004,
    [IxMilia.Dxf.DxfAcadVersion]::R2007,
    [IxMilia.Dxf.DxfAcadVersion]::R2010,
    [IxMilia.Dxf.DxfAcadVersion]::R2013
foreach ($version in $versions) {
    $file.Header.Version = $version
    $strversion = $version.ToString().ToLower()
    if ($strversion -eq "min") {
        $strversion = "r9"
    }
    $filename = Join-Path (pwd) "i.$strversion.dxf"
    $fs = New-Object -TypeName System.IO.FileStream($filename, [System.IO.FileMode]::Create)
    $file.Save($fs)
    $fs.Dispose()
}
