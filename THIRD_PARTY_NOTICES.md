# Third-party notices

EmuVora uses the following independently licensed libraries:

- [Avalonia](https://github.com/AvaloniaUI/Avalonia): MIT.
- [Microsoft.Data.Sqlite / EF Core](https://github.com/dotnet/efcore): MIT.
- [SQLitePCLRaw](https://github.com/ericsink/SQLitePCL.raw): Apache-2.0; SQLite is public domain.
- [SharpCompress](https://github.com/adamhathcock/sharpcompress): MIT; additional codec notices are maintained by that project.
- [SkiaSharp](https://github.com/mono/SkiaSharp): MIT; Skia and native dependencies have their own notices.
- [Svg.Skia](https://github.com/wieslawsoltes/Svg.Skia) 3.0.2: MIT, including its SVG/ShimSkiaSharp dependencies. Imported theme assets remain subject to their authors' licenses; none are bundled.
- Test-only: xUnit, Microsoft.NET.Test.Sdk, Avalonia.Headless.XUnit, under their respective licenses.

Downloaded emulator packages are provided by their original projects. Their bundled license/notice files remain in each installation directory:

- Optional [ES-DE](https://gitlab.com/es-de/emulationstation-de), downloaded as a separate GPL application from its official Windows portable release. Bundled upstream notices remain intact.
- Optional [AlekFull NX ES-DE](https://github.com/anthonycaccese/alekfull-nx-es-de): fagnerpc / anthonycaccese, CC-BY-NC-SA 2.0. Downloaded at runtime; not included in EmuVora binaries or source packages. Its original README attribution is retained.

- [RetroArch](https://www.retroarch.com/) and libretro cores: licenses vary by core.
- [DuckStation](https://github.com/stenzek/duckstation).
- [PCSX2](https://github.com/PCSX2/pcsx2).
- [Dolphin](https://dolphin-emu.org/).
- [PPSSPP](https://github.com/hrydgard/ppsspp).

Online cover requests use [libretro-thumbnails](https://github.com/libretro-thumbnails). Artwork rights remain with the original rights holders. No artwork is bundled with EmuVora.

This notice is an index, not a replacement for upstream license texts. Refer to upstream repositories and the downloaded package notices for complete license conditions.

## FluentAvaloniaUI 2.5.1

[FluentAvalonia](https://github.com/amwx/FluentAvalonia), used for Avalonia 11 controls and styling. The upstream MIT license follows:

MIT License

Copyright (c) 2025 amwx

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.

## Optional ROM recognition metadata

The optional metadata feature downloads original No-Intro DATs from https://github.com/libretro/libretro-database at commit 12c21cf79f1e20eff6ac68b80875816c717d0a7a. Upstream repository license: CC-BY-SA-4.0, https://creativecommons.org/licenses/by-sa/4.0/. Source metadata and original headers remain in the local cache. Only source URLs and content hashes are included in EmuVora; no complete ROM catalogs or ROM data are bundled. See ROM_RECOGNITION.md for attribution and supported scope.
