# FastyPDF

Windows desktop PDF tools. Native WinUI 3 app. All processing stays 100% on your device.

![FastyPDF](fastypdf.png)

## Features

- **Merge PDFs**: Combine multiple PDF files in any order.
- **Split PDFs**: Extract specific page ranges or split into single pages.
- **Organize Pages**: Reorder, rotate, and delete pages with full Undo / Redo support.
- **PDF Reader**: Built-in fast PDF viewer with continuous scroll, dual-page view, dark mode, text search, and zoom.
- **Images to PDF**: Convert JPG, PNG, BMP, TIFF, and WebP into a single PDF document.
- **PDF to Images**: Export PDF pages to high-resolution PNG or JPEG images.
- **Native Fluent UI**: Clean WinUI 3 interface with dark/light theme and Mica backdrop.
- **100% Offline & Private**: Zero cloud dependency, no telemetry, all processing stays local.

## Architecture

FastyPDF follows the MVVM pattern and is split into two main projects:

- `FastyPDF.Core` — Platform-independent PDF logic, services, and models. Contains all PDF manipulation, rendering, and text extraction backed by PDFium and PDFsharp.
- `FastyPDF` — WinUI 3 application layer with views, view models, navigation, and platform services.

## Keyboard Shortcuts

### General

| Shortcut | Action |
|---|---|
| `Ctrl+O` | Open a PDF file |
| `Ctrl+W` | Close the current document |

### PDF Reader

| Shortcut | Action |
|---|---|
| `Left` / `PageUp` | Previous page |
| `Right` / `PageDown` | Next page |
| `Home` / `End` | First / last page |
| `Ctrl+Mouse Wheel` | Zoom in / out |
| `Ctrl++` / `Ctrl+-` | Zoom in / out |
| `Ctrl+0` | Reset zoom to 100% |
| `F11` | Toggle full screen |
| `Ctrl+B` | Toggle thumbnails panel |
| `Ctrl+F` | Open text search |
| `Ctrl+C` | Copy current page text |
| `Ctrl+D` | Document properties |
| `Ctrl+P` | Print |

### Page Organizer

| Shortcut | Action |
|---|---|
| `Ctrl+Z` | Undo |
| `Ctrl+Y` | Redo |
| `Ctrl+S` | Save organized pages |
| `Delete` | Delete selected pages |

## Requirements

- Windows 10 (version 19041+) or Windows 11
- .NET 8 SDK
- Windows App SDK (automatically restored via NuGet)

## Getting Started

### Build and Run

```bash
# Restore dependencies
dotnet restore

# Build the application (x64)
dotnet build src/FastyPDF/FastyPDF.csproj -c Debug -p:Platform=x64

# Run the app
dotnet run --project src/FastyPDF/FastyPDF.csproj -c Debug -p:Platform=x64
```

### Run Tests

```bash
dotnet test
```

### Build Installer

An NSIS-based setup executable can be generated using:

```powershell
powershell -ExecutionPolicy Bypass -File installer/build_installer.ps1
```
*(Requires NSIS installed and added to PATH)*

## License

MIT. See [LICENSE](LICENSE).
