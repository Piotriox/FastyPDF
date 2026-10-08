# Contributing

Keep changes small, local-first, and focused on the existing PDF tools.

## Setup

1. Install the .NET 8 SDK on Windows.
2. Clone the repository.
3. Restore and build with x64:

```
dotnet restore
dotnet build src/FastyPDF/FastyPDF.csproj -c Debug -p:Platform=x64
```

## Guidelines

- Put PDF logic in `src/FastyPDF.Core`, not in views.
- Keep UI in WinUI 3 / XAML. Do not add a WebView-based interface.
- Do not send user files to a network service.
- Prefer a new service, view model, and page when adding a tool.
- Do not add OCR, e-signature, cloud accounts, or AI features unless that is the agreed scope.
- Match the existing naming, MVVM layout, and error handling.

## Pull requests

- Describe what changed and why.
- Confirm the x64 build succeeds.
- Do not commit `bin/`, `obj/`, or user-specific files. Use the root `.gitignore` only.
