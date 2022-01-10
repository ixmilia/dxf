Changelog
=========

## 0.8.2 (10 January 2022)

- Improve XDATA reading.
- Improve `HATCH` writing.
- Improve compatibility with R12 when writing `BLOCK` and `INSERT` entities.

## 0.8.1 (13 July 2021)

- Expand object handles to 64 bits.
- Improve `DIMENSION` entity writing; better interop with AutoCAD.
- Fix writing `TEXT` alignment.
- Allow XDATA after every entity and object.
- Ensure all items have a valid handle after save.

## 0.8.0 (19 January 2021)

- Only build `netstandard2.0` TFM.
- Add bounding box for `LWPOLYLINE`.
- Improve thumbnail detection.
- Honor code page header setting.

## 0.7.5 (13 November 2020)

- Improve table handling.
- Enable XData for all entities and objects.
- Associate `INSERT` entities by block name.
- Improve handling of binary data.

## 0.7.4 (26 June 2020)

- Relicense as MIT.
- Basic support for `HATCH` entities.
- Improve `DIMENSION` support.
- Allow reading incomplete `LEADER` points.

## 0.7.3 (13 February 2020)

- Fix bug in writing spline weights.
- Fix ARC bounding box calculation.
- Numerous compatibility updates including various encoding and unicode support.
- Support both pre- and post-R13 binary files.

## 0.7.2 (23 October 2018)

- Enable reading files with a long `$ACADMAINTVER` value.
- Add direct file system support to the platforms that allow it.

## 0.7.1 (16 October 2018)

- Enable writing entities with embedded objects.
- Improve writing `VERTEX` and `POLYLINE` entities.
- Add `net35` framework support (thanks to @nzain).

## 0.7 (29 January 2018)

- Embed commit hash in produced binaries.
- Convert `DxfPoint` and `DxfVector` to structs.
- Add simple bounding box computations to file and entities.

## 0.6 (11 December 2017)

- Improve `SEQEND` creation.
- Improve handle assignment on file write.
- Improve reading XDATA code pairs.
- Fix issue with object minimum versions.
- Add support for R2018 (AC1032) files.

## 0.5.1 (26 July 2017)

- Improve reading XData points.

## 0.5 (28 April 2017)

- Doc comment support
- Enforce collection restrictions:
  - Minimum number of children (e.g., `POLYLINE` vertices).
  - Disallow `null`.
  - etc.
- Directly get/set the active view port from the drawing.
- Ignore case on block and table item names.
- Improved `LTYPE` support.
- Convert to .NET Core, including cross-platform.

## 0.4 (6 July 2016)

- Make file contents more closely match those produced by AutoCAD.
- Proper support for item handles, including binding.
- Include support for pre-R9 versions.
- Improved XRECORD handling.

## 0.3.1 (25 November 2015)

- Better handle layer colors.

## 0.3 (6 November 2015)

- Added support for binary DXB files.
- Improved back compat with R10-R13.
- Non-ASCII control characters in strings.
- XDATA.
- Culture invariant.
- Objects.
- More entities:
  - `HELIX`
  - `LIGHT`
  - `MLEADER`
  - `MLINE`
  - `SECTION`
  - `UNDERLAY`

## 0.2 (13 March 2015)

- Support ASCII and binary files, R10 through R2013.
- Simple entity support.
- Blocks.
- Tables (including layers, view ports, etc.)
