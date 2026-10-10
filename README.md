# EmuVora

Open-source Windows game-library frontend inspired by OpenEmu. **v0.1 preview source**, not yet verified by a successful build.

## Features implemented in source
- Avalonia desktop UI with Midnight (default), Classic Light, Retro Green and Neon Purple themes
- Recursive ROM scanner, SQLite library and search
- Configurable external emulator executables and RetroArch core paths
- Game launch through an emulator adapter using safe argument lists
- Official emulator website links
- Windows GitHub Actions build/test/publish workflow and tag-based release workflow

## Not yet implemented
- Automated downloads, updates, signature/hash verification, config generation, controller mapping
- Automatic cover metadata, save states, package installer, gamepad-first UI
- Reliable disc format identification from file content (disc folder heuristics only)

## Requirements
- .NET 10 SDK for building
- Windows 10/11 for the target platform

## Run locally
```powershell
dotnet restore EmuVora.sln
dotnet run --project src/EmuVora.App/EmuVora.App.csproj
```

Import a ROM directory, select a game, configure the relevant emulator executable on the left, then press Play. For RetroArch, configure a matching libretro core file as well. ROMs and BIOS are not included.

## Publish to your public GitHub repository
Create a **public** empty repository named `EmuVora` on GitHub, then from the extracted directory:
```powershell
git init
git add .
git commit -m "Initial EmuVora v0.1 preview"
git branch -M main
git remote add origin https://github.com/YOUR_USERNAME/EmuVora.git
git push -u origin main
```
A successful GitHub Actions build creates a downloadable Windows artifact. Creating a `v0.1.0` tag triggers the release workflow.

## Architecture / security
`EmuVora.Core` owns scanning, SQLite persistence, the emulator catalog and `IEmulatorAdapter`/`IEmulatorPackageProvider` contracts. Future downloads should only accept vetted official sources, validate hashes/signatures, request user consent, and support configuration backup/rollback. Emulator binaries are not redistributed by this project.

## License
MIT for original EmuVora code only; emulator projects retain their respective licenses.
