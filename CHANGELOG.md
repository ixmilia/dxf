Changelog
=========

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
