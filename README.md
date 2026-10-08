# FastyPDF

Windows desktop PDF tools. Native WinUI 3 app. All processing stays on the device.

## Features

- Merge PDFs
- Split PDFs
- Reorder and delete pages
- PDF reader
- Images to PDF
- PDF pages to PNG or JPEG

## Requirements

- Windows 10 version 19041 or later
- .NET 8 SDK
- Windows App SDK (restored with the project)

## Build

```
dotnet restore
dotnet build src/FastyPDF/FastyPDF.csproj -c Debug -p:Platform=x64
```

Run from Visual Studio or:

```
dotnet run --project src/FastyPDF/FastyPDF.csproj -c Debug -p:Platform=x64
```

## License

MIT. See [LICENSE](LICENSE).
