<script setup>
const year = new Date().getFullYear()
</script>

<template>
  <footer class="app-footer">
    <div class="footer-grid">
      <section class="brand">
        <span class="logo">JustOne</span>
        <p class="tagline">One photo per day. Nothing more.</p>
      </section>

      <section>
        <h4>Company</h4>
        <address>
          JustOne GmbH<br />
          Beispielstraße 42<br />
          10115 Berlin<br />
          Germany
        </address>
      </section>

      <section>
        <h4>Contact</h4>
        <ul>
          <li><a href="mailto:hello@justone.example">hello@justone.example</a></li>
          <li><a href="tel:+493012345678">+49 30 1234 5678</a></li>
        </ul>
      </section>

      <section>
        <h4>Legal</h4>
        <ul>
          <li><a href="#">Imprint</a></li>
          <li><a href="#">Privacy Policy</a></li>
          <li><a href="#">Terms of Use</a></li>
        </ul>
      </section>
    </div>

    <div class="footer-bottom">
      <span>&copy; {{ year }} JustOne GmbH · Made with ❤ in Berlin</span>
    </div>
  </footer>
</template>

<style scoped>
.app-footer {
  margin-top: 48px;
  background: var(--color-cyan);
  color: var(--color-dark);
  border-top: 4px solid var(--color-dark);
}

.footer-grid {
  max-width: 960px;
  margin: 0 auto;
  padding: 32px 24px;
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(160px, 1fr));
  gap: 24px;
}

.logo {
  font-size: 1.6rem;
  font-weight: 900;
  letter-spacing: 1px;
}

.tagline {
  margin: 6px 0 0;
  font-weight: 600;
}

h4 {
  margin: 0 0 10px;
  font-size: 0.8rem;
  text-transform: uppercase;
  letter-spacing: 1.5px;
}

h4::after {
  content: '';
  display: block;
  width: 28px;
  height: 3px;
  margin-top: 4px;
  background: var(--color-pink);
  border-radius: 2px;
}

address {
  font-style: normal;
  line-height: 1.6;
}

ul {
  list-style: none;
  margin: 0;
  padding: 0;
  line-height: 1.8;
}

a {
  color: var(--color-dark);
  font-weight: 600;
}

a:hover {
  color: var(--color-pink);
}

.footer-bottom {
  padding: 12px 24px;
  background: var(--color-dark);
  color: var(--color-light);
  text-align: center;
  font-size: 0.85rem;
}
</style>
