// Deterministischer Pseudo-Zufallsgenerator: gleicher Seed = immer gleiche Zahlenfolge.
// Damit bleiben die zufällig verteilten "verpasst"-Tage eines Monats über mehrere
// Aufrufe/Re-Renders hinweg stabil, statt bei jeder Navigation neu gewürfelt zu werden.
export function mulberry32(seed) {
  let a = seed
  return function () {
    a |= 0
    a = (a + 0x6d2b79f5) | 0
    let t = Math.imul(a ^ (a >>> 15), 1 | a)
    t = (t + Math.imul(t ^ (t >>> 7), 61 | t)) ^ t
    return ((t ^ (t >>> 14)) >>> 0) / 4294967296
  }
}

// Wählt `count` eindeutige Tage aus 1..daysInMonth aus (aufsteigend sortiert).
export function pickRandomDays(daysInMonth, count, seed) {
  const rand = mulberry32(seed)
  const days = Array.from({ length: daysInMonth }, (_, i) => i + 1)

  for (let i = days.length - 1; i > 0; i--) {
    const j = Math.floor(rand() * (i + 1))
    ;[days[i], days[j]] = [days[j], days[i]]
  }

  return days.slice(0, count).sort((a, b) => a - b)
}