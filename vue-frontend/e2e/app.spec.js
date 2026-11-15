import { fileURLToPath } from 'node:url'
import { test, expect } from '@playwright/test'

// Step screenshots for the docs: only when E2E_SCREENSHOTS=1 is set, otherwise a no-op.
// They are written to docs/tests/e2e/ and overwrite the previous ones.
const SCREENSHOT_DIR = new URL('../../docs/tests/e2e/', import.meta.url)
// The Vue DevTools overlay is always hidden; toasts only stay when `toasts: true`.
async function snap(page, name, { toasts = false, clipTo, maxHeight = 560, ...options } = {}) {
  if (!process.env.E2E_SCREENSHOTS) return
  const path = fileURLToPath(new URL(`${name}.png`, SCREENSHOT_DIR))
  const style = `#__vue-devtools-container__ { display: none !important; }${toasts ? '' : ' .toast-host { display: none !important; }'}`
  let clip
  if (clipTo) {
    // Crop the full page to one card (from its top, at most maxHeight tall).
    await page.evaluate(() => window.scrollTo(0, 0))
    const box = await clipTo.boundingBox()
    clip = { x: box.x - 16, y: box.y - 16, width: box.width + 32, height: Math.min(box.height, maxHeight) + 32 }
  }
  await page.screenshot({ path, animations: 'disabled', style, fullPage: !!clip, clip, ...options })
}

// A fresh account per run, so "today" is always still free to post.
function newUser() {
  const id = `${Date.now()}${Math.floor(Math.random() * 1000)}`
  return { firstName: 'Erika', lastName: 'Tester', email: `e2e-${id}@example.com`, password: 'E2e-Test1!' }
}

async function register(page, user) {
  await page.goto('/')
  await page.getByRole('link', { name: 'Create an account' }).click()
  await page.getByLabel('First name').fill(user.firstName)
  await page.getByLabel('Last name').fill(user.lastName)
  await page.getByLabel('Email').fill(user.email)
  await page.getByLabel('Password').fill(user.password)
  await page.getByRole('button', { name: 'Create account' }).click()
  await expect(page.getByRole('heading', { level: 1, name: `Hi ${user.firstName}` })).toBeVisible()
}

test('wrong password shows an error and stays on the login page', async ({ page }) => {
  await page.goto('/')
  await page.getByLabel('Email').fill('anna@justone.com')
  await page.getByLabel('Password').fill('definitely-wrong')
  await page.getByRole('button', { name: 'Log in', exact: true }).click()

  await expect(page.getByRole('alert')).toContainText('Email or password is wrong')
  await expect(page.getByRole('heading', { name: 'Welcome back' })).toBeVisible()
  await snap(page, '01-wrong-password')
})

test('demo account logs in and shows the month with seeded posts', async ({ page }) => {
  await page.goto('/')
  await page.getByRole('button', { name: 'Try the demo account' }).click()

  await expect(page.getByRole('heading', { level: 1, name: /^Hi / })).toBeVisible()
  await expect(page.getByText(/\d+ of \d+ days posted/)).toBeVisible()
  await expect(page.getByRole('heading', { name: 'Guestbook' })).toBeVisible()
  await snap(page, '02-demo-month', { fullPage: true })
})

test('register, post a text, comment, sign the guestbook, log out and back in', async ({ page }) => {
  const user = newUser()
  const poem = 'Morning light\non the window,\ncoffee still warm.'

  // Register: logs in right away, nothing posted today yet.
  await register(page, user)
  await expect(page.getByText('You haven’t posted today yet.')).toBeVisible()

  // Post today's text.
  const todayCard = page.locator('.today-card')
  await todayCard.getByRole('radio', { name: 'Text' }).click()
  await page.getByLabel('Your words for today').fill(poem)
  await snap(page, '03-writing-a-text-post')
  await todayCard.getByRole('button', { name: 'Post', exact: true }).click()

  await expect(page.getByText('Posted for today. See you tomorrow!')).toBeVisible()
  await expect(page.getByText('Done for today ✓')).toBeVisible()
  await expect(todayCard.locator('blockquote')).toHaveText(poem)
  await snap(page, '04-posted-for-today', { toasts: true })

  // One post per day: the editor is gone, only change/delete is offered.
  await expect(todayCard.getByRole('button', { name: 'Change post' })).toBeVisible()
  await expect(page.getByLabel('Your words for today')).toHaveCount(0)

  // The calendar shows it; opening it shows the full text and the comments.
  await page.getByRole('button', { name: /^Your text for today/ }).click()
  const dialog = page.getByRole('dialog')
  await expect(dialog).toBeVisible()
  await expect(dialog.locator('blockquote')).toHaveText(poem)
  await expect(dialog.getByText('No comments yet')).toBeVisible()

  await dialog.getByRole('textbox', { name: 'Comment' }).fill('Lovely start of the day!')
  await dialog.getByRole('button', { name: 'Send' }).click()
  const comment = dialog.locator('.comment').filter({ hasText: 'Lovely start of the day!' })
  await expect(comment).toBeVisible()
  await expect(comment.getByText('You', { exact: true })).toBeVisible()
  await expect(comment.getByText('Erika T.')).toBeVisible() // never the email
  await snap(page, '05-comment-on-the-post')

  await page.keyboard.press('Escape')
  await expect(dialog).toHaveCount(0)

  // Guestbook entry, visible with the short author name.
  const message = 'Hello everyone – first day on JustOne!'
  await page.getByLabel('New entry').fill(message)
  await page.getByRole('button', { name: 'Post entry' }).click()
  const entry = page.locator('.entry').filter({ hasText: message })
  await expect(entry).toBeVisible()
  await expect(entry.getByText('Erika T.')).toBeVisible()
  await snap(page, '06-guestbook-entry', {
    clipTo: page.locator('section.card').filter({ has: page.getByRole('heading', { name: 'Guestbook' }) }),
  })

  // Log out and back in: the post is still there (stored on the server).
  await page.getByRole('button', { name: 'Log out' }).click()
  // Back on the login form - not on the register form the account was created with.
  await expect(page.getByRole('heading', { name: 'Welcome back' })).toBeVisible()
  await expect(page.getByRole('button', { name: 'Log out' })).toHaveCount(0)
  await snap(page, '07-logged-out')

  // A fresh page load stays logged out: the tokens were removed from localStorage.
  await page.reload()
  await expect(page.getByRole('heading', { name: 'Welcome back' })).toBeVisible()
  await page.getByLabel('Email').fill(user.email)
  await page.getByLabel('Password').fill(user.password)
  await page.getByRole('button', { name: 'Log in', exact: true }).click()
  await expect(page.getByText('Done for today ✓')).toBeVisible()
  await expect(todayCard.locator('blockquote')).toHaveText(poem)
})

test('today’s post can be deleted after confirming', async ({ page }) => {
  await register(page, newUser())

  const todayCard = page.locator('.today-card')
  await todayCard.getByRole('radio', { name: 'Text' }).click()
  await page.getByLabel('Your words for today').fill('Short-lived thought')
  await todayCard.getByRole('button', { name: 'Post', exact: true }).click()
  await expect(page.getByText('Done for today ✓')).toBeVisible()

  await todayCard.getByRole('button', { name: 'Delete' }).click()
  await expect(page.getByRole('button', { name: 'Delete post' })).toBeVisible()
  await snap(page, '08-confirm-delete')
  await page.getByRole('button', { name: 'Delete post' }).click()

  await expect(page.getByText('Today’s post was deleted. You can post something new.')).toBeVisible()
  await expect(page.getByText('You haven’t posted today yet.')).toBeVisible()
  await expect(page.getByRole('button', { name: /^Today, .* – add a post$/ })).toBeVisible()
  await snap(page, '09-deleted', { toasts: true })
})
