# Projekt i kursen DT191G, Webbutveckling med .NET

## Projektbeskrivning
Detta projekt är ett bokningssystem byggt i ASP.NET Core MVC där företag kan skapa tjänster, generera tidsluckor och ta emot bokningar från kunder. Systemet använder Identity för autentisering samt rollhantering, och Entity Framework Core med SQLite för datalagring. Kunder kan se ledigar tider, boka en tjänst och få automatiska e-postbekräftelse vid både bokning och avbokning. Företag har en egen dashboard där de kan hantera sina tjänster, schema samt bokningar.

## Instruktioner

- Klona projektet från GitHub:
git clone https://github.com/TommyIss/dt191g-project
cd dt191g-project

- Installera nödvändiga NuGet-paket:
dotnet restore

- Konfigurera databasanslutning och SMTP-inställningar för e-postbekräftelser i appsettings.json.

- Generera databas:
dotnet ef migrations add InitialCreate
dotnet ef database update

- Starta applikationen:
dotnet run


## Databasstruktur
Projektet använder Entity Framework Core med SQLite som databas. Databasen innehåller tabeller för användare, företag, tjänster, tidsluckor och bokningar.
### Company
| Fält | Datatyp | Beskrivning |
|------|---------|-------------|
| Id | INT | Unikt id|
| Name | STRING | Företagsnamn |
| Description | STRING | Beskrivning |
| Address | STRING | Gatuadress |
| City | STRING | Stad |
| PhoneNumber | STRING | Telefonnummer |
| Email | STRING | E-postadress |
| IsActive | BOOL | Anger om företaget är aktivt |
| Category | STRING | Kategori/Typ av verksamhet |
| OpeningsHours | STRING | Öppettider |
| CreatedAt | DATETIME | Datum för företagets skapande |
| UpdatedAt | DATETIME | Datum för senaste uppdatering |
| Services | RELATION | En-till-fler relation till tjänster |
| TimeSlots | RELATION | En-till-fler relation till tidsluckor |
| CompanyUsers | RELATION | Fler-till-fler relation till företasanvändare |
| UserId | RELATION | Koppling till företagsanvändare |

### Service
| Fält | Datatyp | Beskrivning |
|------|---------|-------------|
| Id | INT | Unikt id|
| Title | STRING | Tjänstens titel |
| Description | STRING | Beskrivning |
| Duration | INT | Varaktighet i minuter |
| Price | DECIMAL | Pris |
| CompanyId | RELATION | Koppling till företag |
| TimeSlots | RELATION | En-till-fler relation till tidsluckor |
| Bookings | RELATION | En-till-fler relation till bokningar |
| CreatedAt | DATETIME | Datum för tjänstens skapande |
| UpdatedAt | DATETIME | Datum för senaste uppdatering |

### TimeSlot
| Fält | Datatyp | Beskrivning |
|------|---------|-------------|
| Id | INT | Unikt id|
| StartTime | DATETIME | Starttid |
| EndTime | DATETIME | Sluttid |
| ServiceId | RELATION | Koppling till tjänst |
| CompanyId | RELATION | Koppling till företag |
| BookingId | RELATION | Koppling till bokning (0..1) |
| IsBooked | BOOL | Anger om tidsluckan är bokad |
| CreatedAt | DATETIME | Datum för tidsluckans skapande |
| UpdatedAt | DATETIME | Datum för senaste uppdatering |

### Booking
| Fält | Datatyp | Beskrivning |
|------|---------|-------------|
| Id | INT | Unikt id|
| UserId | RELATION | Koppling till användare |
| ServiceId | RELATION | Koppling till tjänst |
| TimeSlotId | RELATION | Koppling till tidslucka en-till-en-relation |
| CompanyId | RELATION | Koppling till företag |
| CustomerName | STRING | Kundens namn |
| CustomerEmail | STRING | Kundens e-postadress |
| CustomerPhone | STRING | Kundens telefonnummer |
| Notes | STRING | Eventuella anteckningar från kunden |
| Status | STRING | Status för bokningen (t.ex. "Bekräftad", "Avbokad") |
| CreatedAt | DATETIME | Datum för bokningens skapande |
| UpdatedAt | DATETIME | Datum för senaste uppdatering |

### CompanyUser
| Fält | Datatyp | Beskrivning |
|------|---------|-------------|
| Id | INT | Unikt id|
| UserId | RELATION | Koppling till användare |
| CompanyId | RELATION | Koppling till företag |
| Role | STRING | Roll för användaren inom företaget (t.ex. "Admin", "Medarbetare") |
| CreatedAt | DATETIME | Datum för kopplingens skapande |

### ApplicationUser från Identity
| Fält | Datatyp | Beskrivning |
|------|---------|-------------|
| Id | STRING | Unikt id |
| UserName | STRING | Användarnamn |
| Email | STRING | E-postadress |
| PasswordHash | STRING | Lösenordshash |


## Eventuella beroende och konfiguration
Projektet använder .NET SDK 10.0, Entity Framework Core, SQLite och ASP.NET Core Identity. Databasanslutning och SMTP-inställningar konfigureras i appsettings.json. EF Core migrations används för att skapa och uppdatera databasen. Känsliga uppgifter som API-nycklar, databasanlutningar och app-lösenord ska exkluderas från versionshantering via .gitignore. 

### Tommy Issa, tois2401