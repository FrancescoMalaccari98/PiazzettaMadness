# Piazzetta Madness App

Applicazione desktop Windows per gestire partite live, tabelloni pubblici, sponsor, statistiche giocatori e 3 Point Contest.

## Stack

- .NET 8 / WPF
- SQLite locale con Entity Framework Core
- Tabelloni HTML/CSS/JavaScript in WebView2

## Avvio

```powershell
dotnet run --project src\PiazzettaMadness.App\PiazzettaMadness.App.csproj
```

## Build

```powershell
dotnet build PiazzettaMadness.sln -c Release
```

## Note Git

Il repository e pensato per l'app desktop. I file generati, le build, i database locali, le configurazioni locali e la cartella `server/` sono esclusi da Git.
