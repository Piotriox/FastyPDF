# Contributing to FastyPDF

Thank you for your interest in contributing. Please keep changes small, focused, and aligned with our offline-first and privacy-focused principles.

## Setup

1. Install the .NET 8 SDK on Windows.
2. Clone the repository.
3. Restore and build with x64:

   ```
   dotnet restore
   dotnet build src/FastyPDF/FastyPDF.csproj -c Debug -p:Platform=x64
   ```

## Project Structure

- `src/FastyPDF.Core` — Platform-independent PDF logic: services, models, abstractions, and exceptions. All PDF manipulation, rendering, and text extraction code lives here.
- `src/FastyPDF` — WinUI 3 application: views, view models, navigation, and platform-specific services such as image encoding and file picking.
- `tests/FastyPDF.Core.Tests` — Unit tests for the core library.

## Guidelines

- Put PDF logic in `src/FastyPDF.Core`, not in views or view models.
- Keep UI in WinUI 3 / XAML. Do not add a WebView-based interface.
- Do not send user files to a network service.
- Prefer a new service, view model, and page when adding a tool.
- Match the existing naming, MVVM layout, and error handling.

## Submitting Pull Requests

1. Make sure all unit tests pass:
   ```bash
   dotnet test
   ```
2. Verify the application builds with 0 warnings:
   ```bash
   dotnet build src/FastyPDF/FastyPDF.csproj -c Release -p:Platform=x64
   ```
3. Submit a pull request describing the problem solved and changes made.
