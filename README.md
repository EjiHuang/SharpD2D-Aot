# SharpD2D-Aot

[![.NET](https://img.shields.io/badge/.NET-10-blueviolet)](https://dotnet.microsoft.com/)
[![Native AOT](https://img.shields.io/badge/Native%20AOT-ready-green)](https://learn.microsoft.com/dotnet/core/deploying/native-aot)
[![License](https://img.shields.io/badge/license-MIT-blue)](LICENSE)

**SharpD2D-Aot** is a .NET library providing managed bindings for [Direct2D](https://learn.microsoft.com/windows/win32/direct2d/direct2d-portal) and [DirectWrite](https://learn.microsoft.com/windows/win32/directwrite/direct-write-portal), refactored on top of [DirectNAot](https://www.nuget.org/packages/DirectNAot) to enable full **Native AOT** compilation support.

> This is a fork of the original [SharpD2D](https://github.com/Sardelka9515/SharpD2D) by **Sardelka**, rewritten to use DirectNAot as the underlying interop layer.

## 📦 Solution Structure

| Project | Description |
|---------|-------------|
| `SharpD2D` | Core library — managed Direct2D/DirectWrite bindings |
| `Examples` | WinForms-based demo (overlay, sticky window, HWnd control) |
| `ExampleAot` | Minimal Native AOT compatible demo — overlay window only |

## 🚀 Getting Started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- Windows 10 / 11 (Direct2D requires Windows)

### Build

```bash
dotnet build SharpD2D.sln
```

### Run the Native AOT Demo

```bash
dotnet run --project ExampleAot
```

### Publish as Native AOT

```bash
dotnet publish ExampleAot -r win-x64 -c Release --self-contained
```

The output is a single `ExampleAot.exe` (~5 MB) with zero .NET runtime dependencies.

## 📄 License

MIT — see [LICENSE](LICENSE) for details.

## 👥 Contributors

| | |
|---|---|
| **[Sardelka](https://github.com/Sardelka9515)** | Original author of SharpD2D |
| **[EjiHuang](https://github.com/EjiHuang)** | DirectNAot refactoring & Native AOT enablement |

See [CONTRIBUTORS.md](CONTRIBUTORS.md) for more details.
