<script setup>
const year = new Date().getFullYear()
</script>

<template>
  <footer class="app-footer">
    <div class="footer-grid">
      <section>
        <span class="brand"><span class="brand-mark" aria-hidden="true">1</span>Just<span class="one">One</span></span>
        <p class="tagline">One photo per day. Nothing more.</p>
      </section>

      <section>
        <h2>Company</h2>
        <address>
          JustOne GmbH<br />
          Beispielstraße 42<br />
          10115 Berlin, Germany
        </address>
      </section>

      <section>
        <h2>Contact</h2>
        <ul>
          <li><a href="mailto:hello@justone.example">hello@justone.example</a></li>
          <li><a href="tel:+493012345678">+49 30 1234 5678</a></li>
        </ul>
      </section>

      <section>
        <h2>Legal</h2>
        <!-- Placeholder pages don't exist yet, so these are plain text instead of dead "#" links. -->
        <ul class="muted">
          <li>Imprint (coming soon)</li>
          <li>Privacy (coming soon)</li>
          <li>Terms (coming soon)</li>
        </ul>
      </section>
    </div>

    <div class="footer-bottom">&copy; {{ year }} JustOne GmbH · Berlin</div>
  </footer>
</template>

<style scoped>
.app-footer {
  margin-top: var(--space-7);
  background: var(--surface-inverse);
  color: var(--text-inverse);
  border-top: 3px solid var(--brand-on-dark);
  font-size: var(--text-sm);
}

.footer-grid {
  max-width: 1120px;
  margin: 0 auto;
  padding: var(--space-6) 24px;
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(170px, 1fr));
  gap: var(--space-5);
}

.brand {
  display: flex;
  align-items: center;
  gap: 10px;
  font-size: var(--text-lg);
  font-weight: 700;
}

.brand-mark {
  width: 32px;
  height: 32px;
  display: grid;
  place-items: center;
  border-radius: 8px;
  background: var(--accent);
  color: var(--text-on-accent);
}

.one {
  color: var(--brand-on-dark);
  margin-left: -10px;
}

.tagline {
  margin: var(--space-2) 0 0;
  color: var(--text-inverse-secondary);
}

h2 {
  margin: 0 0 var(--space-2);
  font-size: var(--text-xs);
  font-weight: 600;
  text-transform: uppercase;
  letter-spacing: 0.08em;
  color: var(--text-inverse-secondary);
}

address {
  font-style: normal;
}

ul {
  list-style: none;
  margin: 0;
  padding: 0;
  display: grid;
  gap: 4px;
}

a {
  color: var(--brand-on-dark);
  text-underline-offset: 3px;
}

.muted {
  color: var(--text-inverse-secondary);
}

.footer-bottom {
  padding: var(--space-3);
  border-top: 1px solid rgba(255, 255, 255, 0.1);
  text-align: center;
  color: var(--text-inverse-secondary);
  font-size: var(--text-xs);
}
</style>
