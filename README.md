# FastyPDF

Windows desktop PDF tools. Native WinUI 3 app. All processing stays 100% on your device.

![FastyPDF](fastypdf.png)

## Features

- **Merge PDFs**: Combine multiple PDF files in any order.
- **Split PDFs**: Extract specific page ranges or split into single pages.
- **Organize Pages**: Reorder, rotate, and delete pages with full Undo / Redo support.
- **PDF Reader**: Built-in fast PDF viewer with zoom and page navigation.
- **Images to PDF**: Convert JPG, PNG, BMP, TIFF, and WebP into a single PDF document.
- **PDF to Images**: Export PDF pages to high-resolution PNG or JPEG images.
- **Native Fluent UI**: Clean WinUI 3 interface with dark/light theme and Mica backdrop.
- **100% Offline & Private**: Zero cloud dependency, no telemetry, all processing stays local.

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
