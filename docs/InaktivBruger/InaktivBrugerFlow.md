# Inaktiv Bruger Flow

Dette dokument beskriver det komplette flow for automatisk sletning af inaktive brugere i Pantio — fra aktivitetssporing og in-app advarsel til endelig sletning efter 12 måneders inaktivitet.

---

## Overblik

Der er tre sammenhængende flows:

1. **Aktivitetssporing**: Hver gang en bruger anvender appen, opdateres `last_activity_at` på brugeren. Dette sker automatisk via `Auth0OwnershipFilter` — brugeren behøver ikke gøre noget aktivt.
2. **Advarsel**: En baggrundstjeneste kører dagligt og finder brugere der har været inaktive i 11+ måneder. Disse brugere markeres med `deletion_warning_sent_at`, som frontend'en læser og viser en advarselsbanner for.
3. **Sletning**: Samme baggrundstjeneste sletter brugere der har været inaktive i 12+ måneder — både fra databasen og fra Auth0.

---

## Entiteter og tabeller

| Entitet | Tabel | Fil |
|---------|-------|-----|
| `User` | `users` | `PantioClassLibrary/Entities/User.cs` |

### Nye kolonner på `users`

| Kolonne | Type | Beskrivelse |
|---------|------|-------------|
| `last_activity_at` | timestamp? | Tidspunkt for brugerens seneste API-kald. `null` for brugere oprettet før featurens deployment — `created_at` bruges som fallback. |
| `deletion_warning_sent_at` | timestamp? | Sættes når advarslen udsendes (11 måneder). `null` = ingen advarsel sendt endnu. |

Migration: `PantioRepository/EntityFramework/EFMigrations/20260511121636_AddInactiveUserTracking.cs`

---

## Flow 1 — Aktivitetssporing

### Trigger
Enhver autentificeret API-anmodning fra appen.

### Trin-for-trin

**Trin 1 — Valider JWT og opløs bruger**
`PantioAPI/Filters/Auth0OwnershipFilter.cs` (`OnActionExecutionAsync`)

`sub`-claimen fra JWT udtrækkes og slås op i `users`-tabellen via `IUserRepository.GetByAuth0SubAsync`. Returnerer `401 Unauthorized` hvis brugeren ikke kendes.

---

**Trin 2 — Opdatér aktivitetstidspunkt (max én gang per dag)**
`PantioAPI/Filters/Auth0OwnershipFilter.cs` ->
`PantioRepository/EntityFramework/Repositories/UserRepository.cs` (`UpdateLastActivityAsync`)

Filteren sammenligner `user.LastActivityAt?.Date` med `DateTime.UtcNow.Date`. Kun hvis datoen er forskellig (eller `LastActivityAt` er `null`) skrives en opdatering til databasen. Dette begrænser DB-skrivning til maksimalt én gang pr. dag pr. bruger uanset appbrug.

`UpdateLastActivityAsync` bruger `ExecuteUpdateAsync` for at undgå at hente og tracke hele entiteten.

---

## Flow 2 — Advarsel (11 måneder)

### Trigger
`InactiveUserBackgroundService` kører dagligt (interval: 24 timer).

### Trin-for-trin

**Trin 1 — Find brugere til advarsel**
`PantioAPI/Services/InactiveUserService.cs` (`RunCheckAsync`) ->
`PantioRepository/EntityFramework/Repositories/UserRepository.cs` (`GetUsersToWarnAsync`)

Henter alle brugere hvor:
- `(last_activity_at ?? created_at) <= nu - 11 måneder`
- `deletion_warning_sent_at IS NULL`

Brugere der allerede er advaret springes over.

---

**Trin 2 — Stamp advarselstidspunkt**
`PantioAPI/Services/InactiveUserService.cs` ->
`PantioRepository/EntityFramework/Repositories/UserRepository.cs` (`SetDeletionWarningSentAsync`)

`deletion_warning_sent_at` sættes til `DateTime.UtcNow` via `ExecuteUpdateAsync`. Hændelsen logges med bruger-ID og inaktivitetsdato.

---

**Trin 3 — Frontend viser advarselsbanner**

`GET /api/users/{userId}` returnerer `UserDto` med feltet `deletionWarningSentAt`. Når dette felt er non-null, bør frontend'en vise en synlig advarsel til brugeren om at kontoen slettes om under én måned, og opfordre brugeren til at anvende appen for at nulstille tidslinjen.

---

## Flow 3 — Automatisk sletning (12 måneder)

### Trigger
Samme daglige kørsel af `InactiveUserBackgroundService` — sletning sker i samme `RunCheckAsync` som advarselsfasen, men med 12-månedersgrænsen.

### Trin-for-trin

**Trin 1 — Find brugere til sletning**
`PantioAPI/Services/InactiveUserService.cs` ->
`PantioRepository/EntityFramework/Repositories/UserRepository.cs` (`GetUsersToDeleteAsync`)

Henter alle brugere hvor:
- `(last_activity_at ?? created_at) <= nu - 12 måneder`

---

**Trin 2 — Slet bruger fra Auth0**
`PantioAPI/Services/InactiveUserService.cs` ->
`PantioAPI/Services/Auth0ManagementService.cs` (`DeleteUserAsync`)

Kalder Auth0 Management API med brugerens `auth0_sub`. Hvis dette kald fejler, logges fejlen og brugeren springes over — batch'en fortsætter for de øvrige brugere.

---

**Trin 3 — Slet bruger fra databasen**
`PantioAPI/Services/InactiveUserService.cs` ->
`PantioRepository/EntityFramework/Repositories/UserRepository.cs` (`DeleteAsync`)

Fjerner brugerrækken fra `users`-tabellen. FK-cascade sletter automatisk alle brugerens tilknyttede data: lagervarer, indkøbslister, kvitteringer, opskrifter, udløbsnotifikationer og butiksforbindelser.

---

## Arkitektur

```
Auth0OwnershipFilter  ->  IUserRepository  ->  DbContext  ->  PostgreSQL
InactiveUserBackgroundService  ->  IInactiveUserService  ->  IUserRepository + IAuth0ManagementService
```

| Lag | Fil |
|-----|-----|
| Filter (aktivitetssporing) | `PantioAPI/Filters/Auth0OwnershipFilter.cs` |
| Baggrundstjeneste | `PantioAPI/InactiveUserBackgroundService.cs` |
| Service interface | `PantioClassLibrary/Interfaces/Services/IInactiveUserService.cs` |
| Service impl. | `PantioAPI/Services/InactiveUserService.cs` |
| Repository interface | `PantioClassLibrary/Interfaces/Repository/IUserRepository.cs` |
| Repository impl. | `PantioRepository/EntityFramework/Repositories/UserRepository.cs` |

---

## DTOs

| DTO | Retning | Felt | Beskrivelse |
|-----|---------|------|-------------|
| `UserDto` | Response | `deletionWarningSentAt` | `null` = ingen advarsel. Non-null = advarsel udsendt, vis banner. |

---

## DI-registrering

`PantioAPI/Program.cs`

```csharp
builder.Services.AddScoped<IInactiveUserService, InactiveUserService>();
builder.Services.AddHostedService<InactiveUserBackgroundService>();
```

---

## Vigtige designbeslutninger

**Aktivitet spores på alle API-kald, ikke kun login.**
Da Pantio er en mobilapp er brugere altid logget ind — der er ingen eksplicit login-begivenhed. `Auth0OwnershipFilter` kører på alle autentificerede requests, og er derfor det rette sted at spore aktivitet. Skrivning throttles til én gang per dag for at undgå unødige DB-skrivninger.

**`created_at` som fallback for ældre brugere.**
For brugere oprettet før denne feature blev deployed er `last_activity_at` `null`. I stedet for at anse dem som inaktive fra deploydagen bruges `created_at` som aktivitetsdato. Dette giver eksisterende brugere samme 12-måneders frist som nye.

**Fejl på enkeltbrugere stopper ikke batch'en.**
Sletning af én bruger pakkes i try/catch. Hvis Auth0-kaldet fejler (midlertidig netværksfejl e.l.) logges fejlen og næste bruger behandles. Brugeren forsøges slettet igen ved næste daglige kørsel.

**In-app advarsel via `UserDto` — ikke en separat notifikationsentitet.**
Advarslen er en simpel timestamp på brugeren selv. Frontend'en læser `deletionWarningSentAt` fra det eksisterende `GET /api/users/{userId}`-endpoint. Der oprettes ingen separat notifikationsrække. E-mail kan tilføjes senere ved at kalde en e-mail-service i `InactiveUserService.RunCheckAsync` i advarselsfasen.

**Sletning er permanent og cascade.**
Der er ingen soft-delete. Når en bruger slettes fjernes alle data permanent via FK-cascade i PostgreSQL. Dette er bevidst — formålet er dataminimering.
