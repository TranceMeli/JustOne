<script setup>
// Icons from Google's Material Symbols (Outlined). The font in assets/fonts only contains the
// icons listed below - to add one, download a new subset (see README, "Icons").
// Decorative by default - give the surrounding button a text or aria-label instead.
// An <i> element is used on purpose: components style their text with `span` selectors.
defineProps({
  name: { type: String, required: true }
})

// App name -> Material Symbols ligature name
const glyphs = {
  prev: 'chevron_left',
  next: 'chevron_right',
  close: 'close',
  plus: 'add',
  check: 'check',
  upload: 'upload',
  alert: 'error',
  chat: 'chat_bubble',
  logout: 'logout',
  trash: 'delete',
  refresh: 'refresh',
  sun: 'light_mode',
  moon: 'dark_mode',
  system: 'computer',
  image: 'image',
  text: 'notes',
  quote: 'format_quote'
}
</script>

<template>
  <i class="icon" :data-icon="glyphs[name]" aria-hidden="true" />
</template>
