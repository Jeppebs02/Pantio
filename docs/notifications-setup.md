# Push Notifikationer – Opsætning og Flow

Pantio bruger **Capacitors Push Notifications plugin** kombineret med **Firebase Cloud Messaging (FCM)** til at sende push-notifikationer til Android og iOS.

---

## Arkitektur

```
Backend (ExpiryCheckService)
        │
        ▼
  FcmService.cs  ──── OAuth2 Service Account ────▶  FCM API v1
                                                          │
                                               ┌──────────┴──────────┐
                                               ▼                     ▼
                                       FCM (Android)           APNs (iOS)
                                               │                     │
                                               └──────────┬──────────┘
                                                          ▼
                                                    Enheden
                                                          │
                                                          ▼
                                          usePushNotifications.ts
                                           (toast / foreground)
```

---

## Relevante filer

### Frontend (Ionic/Vue + Capacitor)

| Fil | Formål |
|-----|--------|
| `frontend/PantioApp/src/composables/usePushNotifications.ts` | Kerne-composable: tilladelser, registrering, token-opsamling, toast ved modtagelse |
| `frontend/PantioApp/src/services/users.ts` | `saveFcmToken()` – sender token til backend via `PATCH /api/users/{id}/fcm-token` |
| `frontend/PantioApp/src/App.vue` | Initialiserer composablen ved app-start |
| `frontend/PantioApp/capacitor.config.ts` | App ID: `com.pantio.app` |
| `frontend/PantioApp/android/app/google-services.json` | Firebase-konfiguration for Android (projekt: `pantionofications`) |
| `frontend/PantioApp/ios/App/Podfile` | Erklærer `CapacitorPushNotifications`-pod |

### Backend (.NET 10 / C#)

| Fil | Formål |
|-----|--------|
| `backend/PantioAPI/Services/FcmService.cs` | Sender notifikationer via FCM HTTP v1 API med OAuth2 |
| `backend/PantioAPI/FcmOptions.cs` | Konfigurationsklasse for Firebase service account |
| `backend/PantioAPI/firebase-service-account.json` | Service account credentials (hemmelighed, git-ignoreret) |
| `backend/PantioAPI/Services/ExpiryCheckService.cs` | Baggrundstjeneste der opdager udløb og trigger notifikationer |
| `backend/PantioAPI/Services/ExpiryCheckBackgroundService.cs` | Hosted service der kører `ExpiryCheckService` på interval |
| `backend/PantioClassLibrary/Entities/User.cs` | Kolonne `fcm_token` (nullable string) |
| `backend/PantioRepository/.../UserRepository.cs` | `UpdateFcmTokenAsync(userId, token)` |
| `backend/PantioAPI/Controllers/UserController.cs` | `PATCH /api/users/{id}/fcm-token` – gemmer token i databasen |
| `backend/PantioClassLibrary/Entities/ExpiryNotification.cs` | Audit-tabel: logger sendte notifikationer (kanal, tidspunkt, m.m.) |

---

## End-to-end flow

### 1. App-start og token-registrering

1. `App.vue` initialiserer `usePushNotifications()` ved opstart.
2. Composablen venter på at brugeren er autentificeret (Auth0).
3. Kald til `PushNotifications.requestPermissions()` – brugeren accepterer notifikationer.
4. Kald til `PushNotifications.register()` → Capacitor trigger native registrering.
5. Firebase (Android) / APNs (iOS) genererer et unikt **FCM-token**.
6. `registration`-eventet fyres i Capacitor med tokenet.
7. `usePushNotifications.ts` fanger tokenet og kalder `saveFcmToken(userId, token)`.
8. Backend-endpoint `PATCH /api/users/{id}/fcm-token` gemmer tokenet i `users.fcm_token` (PostgreSQL).

### 2. Baggrundstjek og afsendelse (backend)

1. `ExpiryCheckBackgroundService` kører i loop med **4,8 timers interval**.
2. Hvert kald opretter en scoped `ExpiryCheckService` og kalder `RunCheckAsync()`.
3. Servicen forespørger databasen for udløbsdatoer inden for de næste **3 dage**.
4. For hvert resultat bygges en dansk notifikationstekst:
   - *1 dag tilbage:* `"{Produkt} udløber i morgen"`
   - *N dage tilbage:* `"{Produkt} udløber om {N} dage"`
   - *Udløbet:* `"{Produkt} er udløbet"`
5. Brugerens FCM-token hentes: `expiry.InventoryItem.Inventory.User.FcmToken`.
6. Hvis token findes → kald til `FcmService.SendAsync(token, "Pantio", body)`.
7. Fejler FCM → falder tilbage til `NotificationChannel.InApp`.
8. Resultatet logges i `ExpiryNotification`-tabellen.

### 3. FCM-afsendelse (FcmService)

1. `FcmService` bruger service account-credentials til at hente et **OAuth2 access token**.
2. En HTTP POST sendes til FCM v1 API:
   ```
   POST https://fcm.googleapis.com/v1/projects/pantionofications/messages:send
   ```
   med payload:
   ```json
   {
     "message": {
       "token": "<FCM_TOKEN>",
       "notification": { "title": "Pantio", "body": "..." },
       "android": { "priority": "high" }
     }
   }
   ```
3. Firebase router notifikationen til enheden via FCM (Android) eller APNs (iOS).

### 4. Modtagelse på enheden

- **Baggrund / lukket app**: Systemet viser notifikationen i notifikationscenteret.
- **Forgrunden**: `pushNotificationReceived`-eventet fyres i `usePushNotifications.ts`, der viser en toast via `PToast`-komponenten (`src/components/ui/PToast.vue`).

> **Bemærk:** Der er i øjeblikket ikke implementeret deep linking ved tryk på notifikationen. Et tryk åbner blot appen til forsiden.

---

## Konfiguration

| Indstilling | Værdi | Fil |
|-------------|-------|-----|
| App ID | `com.pantio.app` | `capacitor.config.ts` |
| Firebase-projekt | `pantionofications` | `google-services.json`, `appsettings.json` |
| FCM API-version | v1 | `FcmService.cs` |
| Tjekinterval | 4,8 timer | `appsettings.json` → `ExpiryCheck:IntervalHours` |
| Notifikationshorisont | 3 dage før udløb | `appsettings.json` → `ExpiryCheck:NotificationThresholdDays` |
| Android-prioritet | `high` | `FcmService.cs` |

### appsettings.json (uddrag)
```json
"ExpiryCheck": {
  "NotificationThresholdDays": 3,
  "IntervalHours": 4.8
},
"Firebase": {
  "ProjectId": "pantionofications",
  "ServiceAccountPath": "firebase-service-account.json"
}
```

---

## Database

Tre tabeller er involveret i notifikationsflowet:

- **`users`** – kolonnen `fcm_token` gemmer det aktuelle token per bruger.
  - Migrering: `20260513120013_AddUserFcmToken`
- **`expiry_dates`** – tidspunktet for seneste notifikation logges direkte på rækken.
- **`expiry_notifications`** – audit-log over alle sendte notifikationer med: bruger-ID, udløbsdato-ID, dage tilbage, kanal (`Push`/`InApp`), afsendingstidspunkt og `acknowledged`-flag.

---

## Kendte begrænsninger

- **Ingen deep linking** – tryk på notifikation navigerer ikke til det relevante produkt.
- **Ingen retry-logik** – fejler FCM-kaldet, forsøges det ikke igen (falder til in-app).
- **Token-refresh** – håndteres ved at appen re-registrerer ved hvert login, hvilket overskriver tokenet i databasen.
- **iOS-credentials** – `GoogleService-Info.plist` er ikke committet (git-ignoreret). Skal være til stede i Xcode-projektet for APNs at virke.
