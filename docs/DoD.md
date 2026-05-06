Definition of Done (DoD)
En user story betragtes som færdig når alle nedenstående punkter er opfyldt:

Funktionalitet
 Alle acceptkriterier for storyen er opfyldt og verificeret
 Koden er testet og fungerer på iOS 16+, Android 11+ samt nyeste versioner af Chrome og Safari (T-03)
Kode og versionsstyring
 Al kode er under Git-versionsstyring med meningsfulde commit-beskeder (M-02)
 Feature-branch er anvendt og merget korrekt
 Ingen forretningslogik i præsentationslaget; frontend og backend er struktureret i moduler/lag (M-01)
Sikkerhed
 Al kommunikation foregår over HTTPS/TLS 1.2+; ukrypterede HTTP-forbindelser afvises (S-01)
 Adgangskoder, tokens og personoplysninger optræder ikke i nogen logfil (L-03)
 Input valideres for at beskytte mod SQL-injection, XSS og CSRF (S-05)
Performance
 Sidevisninger og navigationsskift indlæses på under 2 sekunder ved 4G/WiFi (P-01)
 95% af API-kald besvares inden for 500 ms under normale driftsforhold (P-02)
Brugervenlighed og design
 Layoutet fungerer korrekt på skærme fra 360px til 428px bredde uden overflow eller overlappende elementer (B-03)
 Fejlmeddelelser er på dansk, beskriver hvad der gik galt og hvad brugeren kan gøre — ingen rå fejlkoder vises (B-04)
 Farvekontrast overholder WCAG 2.1 AA-standard (min. kontrastforhold 4,5:1 for læsetekst) (T-01)
 Alle interaktive elementer har semantiske labels kompatible med iOS VoiceOver og Android TalkBack (T-02)
GDPR og databeskyttelse
 Brugerdata opbevares inden for EU (L-02)
 Privatlivspolitik er tilgængelig i appen (L-02)
 Brugeren kan til enhver tid anmode om sletning af alle persondata (L-02)
Logging
 Alle 5xx-fejl og uventede undtagelser logges med tidsstempel, endpoint og fejltype (L-01)
 Logs opbevares i minimum 30 dage (L-01)