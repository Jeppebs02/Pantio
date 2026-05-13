<script setup lang="ts">
import AppShell from '../../components/layout/AppShell.vue'
import TopBar from '../../components/layout/TopBar.vue'
</script>

<template>
  <AppShell>
    <template #topbar>
      <TopBar title="Privatliv & Data" :back-route="{ name: 'settings' }" />
    </template>

    <div class="page">
      <section class="card">
        <h2>Hvad gemmer vi?</h2>
        <p>
          Pantio gemmer kun de oplysninger, der er nødvendige for at appen kan fungere. Det drejer sig om:
        </p>
        <ul>
          <li><strong>Din e-mail</strong> — bruges til login via Auth0 og til at sende varsler om udløbsdatoer.</li>
          <li><strong>Dine beholdninger og varer</strong> — produktnavne, mængder, udløbsdatoer og opbevaringssteder, som du selv indtaster eller importerer.</li>
          <li><strong>Stregkodeopslag</strong> — når du scanner en stregkode, forespørger vi Open Food Facts. Vi gemmer resultatet lokalt, så du ikke behøver scanne den samme vare igen.</li>
          <li><strong>Butiksintegration</strong> — hvis du tilslutter Netto, gemmes et krypteret adgangstoken, så vi kan hente dine kvitteringer automatisk.</li>
          <li><strong>Aktivitetstidspunkt</strong> — vi registrerer hvornår du sidst brugte appen. Konti der har været inaktive i over 90 dage varsles og slettes automatisk.</li>
        </ul>
      </section>

      <section class="card">
        <h2>Hvem har adgang?</h2>
        <p>
          Dine data er kun tilgængelige for dig. Vi deler ikke personlige oplysninger med tredjeparter. Vi bruger følgende underleverandører til at drive tjenesten:
        </p>
        <ul>
          <li><strong>Auth0</strong> — håndterer login og identitet sikkert.</li>
          <li><strong>Open Food Facts</strong> — åben produktdatabase til stregkodeopslag. Ingen persondata sendes.</li>
          <li><strong>Google Gemini</strong> — bruges til at generere opskriftsforslag. Der sendes kun produktnavne, ikke personoplysninger.</li>
        </ul>
      </section>

      <section class="card">
        <h2>Hvordan beskyttes dine data?</h2>
        <ul>
          <li>Al kommunikation sker over HTTPS.</li>
          <li>Butiksadgangstokens gemmes krypteret i databasen.</li>
          <li>Adgang til API'et kræver login — andre brugere kan ikke se dine data.</li>
        </ul>
      </section>

      <section class="card">
        <h2>Sletning af data</h2>
        <p>
          Du kan til enhver tid slette din konto under "Farezonen" på denne side. Det fjerner alle dine data permanent og kan ikke fortrydes. Konti der er inaktive i mere end 90 dage modtager en advarsel og slettes automatisk efter 30 dage, medmindre du logger ind igen.
        </p>
      </section>

      <section class="card">
        <h2>Kontakt</h2>
        <p>
          Har du spørgsmål til dine data eller ønsker indsigt, kan du kontakte os på
          <a href="mailto:kontakt@pantio.dk" class="link">kontakt@pantio.dk</a>.
        </p>
      </section>

      <p class="updated">Sidst opdateret: maj 2026</p>
    </div>
  </AppShell>
</template>

<style scoped>
.page {
  padding: var(--space-4);
  max-width: var(--max-width);
  margin: 0 auto;
  display: flex;
  flex-direction: column;
  gap: var(--space-4);
}

.card {
  background: var(--surface);
  border: 1px solid var(--border);
  border-radius: var(--radius-lg);
  padding: var(--space-5);
  display: flex;
  flex-direction: column;
  gap: var(--space-3);
}

.card h2 {
  font-size: 16px;
  font-weight: 700;
  color: var(--fg);
}

.card p,
.card li {
  font-size: 14px;
  color: var(--fg-muted);
  line-height: 1.6;
}

.card ul {
  display: flex;
  flex-direction: column;
  gap: var(--space-2);
  padding-left: var(--space-5);
}

.link {
  color: var(--sage-600);
  text-decoration: underline;
}

.updated {
  font-size: 12px;
  color: var(--fg-faint);
  text-align: center;
  padding-bottom: var(--space-4);
}
</style>
