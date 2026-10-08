# FastyPDF Architecture

FastyPDF is a local-first Windows desktop PDF toolbox built with WinUI 3 and the Windows App SDK. All document processing runs on the device. There are no network calls and no third-party document uploads.

## Library choices

| Concern | Library | License | Why |
| --- | --- | --- | --- |
| Merge, split, page order, image-to-PDF | PDFsharp 6 (empira) | MIT | Commercial-friendly page import/create API |
| Viewer, thumbnails, PDF-to-image, encryption detection | PDFiumCore + PDFium (bblanchon) | Apache 2.0 | Chromium renderer; native Windows x64 |
| Images (decode WEBP/TIFF/BMP/PNG/JPEG) | Windows Imaging Component | OS | No extra image-library license |
| MVVM | CommunityToolkit.Mvvm | MIT | Observable properties and commands |

Rejected for v1: iText (AGPL), QuestPDF (commercial threshold), Patagames Pdfium.NET (EULA excludes competing PDF apps), WebView2-based viewers.

PDFium is not thread-safe. All PDFium calls go through a single `SemaphoreSlim` in `PdfiumRuntime`. Rendering still happens off the UI thread.

## Layers

- **Views / XAML** — layout, pickers, drag-and-drop, keyboard accelerators
- **ViewModels** — state, commands, validation, progress
- **FastyPDF.Core** — PDF/file/settings/logging services

A new tool is added by implementing or extending a Core service, adding a ViewModel + Page, and registering the tool in `ToolCatalog`.

## Data locations

Settings, recent-file list, and logs live under `%LocalAppData%\FastyPDF`. Original input files are never overwritten; outputs are new files with collision-safe names.
