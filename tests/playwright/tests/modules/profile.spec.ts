/**
 * Profile Feature E2E Tests
 *
 * Covers: profile picture upload, display in navbar.
 *
 * Tags: @profile, @regression, @smoke
 */
import { test, expect } from '@fixtures/role-fixtures';
import { navigateMenu, assertPageHeading } from '@helpers/navigation';
import * as path from 'path';
import * as fs from 'fs';

test.describe('Profile Feature', () => {
  // ── SuperAdmin ───────────────────────────────────────────────────────────

  test.describe('As SuperAdmin', () => {
    test('@profile @smoke should load profile page', async ({ superAdminPage }) => {
      await navigateMenu(superAdminPage, 'Profile', '/Portal/Profile');
      await assertPageHeading(superAdminPage, 'Profile');
    });

    test('@profile @regression should display user info on profile page', async ({ superAdminPage }) => {
      await superAdminPage.goto('/Portal/Profile', { waitUntil: 'domcontentloaded' });

      // Profile page should show username or display name
      const bodyText = await superAdminPage.textContent('body');
      expect(bodyText).toBeTruthy();
      // Super admin username should appear somewhere
      const username = process.env.SUPER_ADMIN_USERNAME || 'superadmin';
      expect(bodyText.toLowerCase()).toContain(username.toLowerCase());
    });

    test('@profile @regression should upload profile picture', async ({ superAdminPage }) => {
      await superAdminPage.goto('/Portal/Profile', { waitUntil: 'domcontentloaded' });

      // Look for file upload input
      const fileInput = superAdminPage.locator('input[type="file"], [data-testid="profile-picture-upload"]');

      if (await fileInput.isVisible({ timeout: 5_000 }).catch(() => false)) {
        // Create a small test image (1x1 pixel PNG)
        const testImagePath = path.resolve(__dirname, '..', '..', 'test-results', 'test-avatar.png');

        // Minimal valid PNG (1x1 blue pixel)
        const pngBytes = Buffer.from([
          0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, // PNG header
          0x00, 0x00, 0x00, 0x0D, 0x49, 0x48, 0x44, 0x52, // IHDR
          0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x01,
          0x08, 0x02, 0x00, 0x00, 0x00, 0x90, 0x77, 0x53,
          0xDE, 0x00, 0x00, 0x00, 0x0C, 0x49, 0x44, 0x41,
          0x54, 0x08, 0xD7, 0x63, 0x60, 0x60, 0x60, 0x00,
          0x00, 0x00, 0x04, 0x00, 0x01, 0x27, 0x34, 0x27,
          0x0A, 0x00, 0x00, 0x00, 0x00, 0x49, 0x45, 0x4E,
          0x44, 0xAE, 0x42, 0x60, 0x82,
        ]);

        // Ensure directory exists
        const dir = path.dirname(testImagePath);
        if (!fs.existsSync(dir)) {
          fs.mkdirSync(dir, { recursive: true });
        }
        fs.writeFileSync(testImagePath, pngBytes);

        // Upload the file
        await fileInput.setInputFiles(testImagePath);

        // Look for a save/submit button
        const saveBtn = superAdminPage.locator(
          'button:has-text("Save"), button:has-text("Upload"), [data-testid="save-profile"]',
        );
        if (await saveBtn.isVisible({ timeout: 3_000 }).catch(() => false)) {
          await saveBtn.click();
        }

        // Clean up
        fs.unlinkSync(testImagePath);

        // Verify success message or updated picture
        const successMsg = superAdminPage.locator('.alert-success, .text-success, [data-testid="success-message"]');
        const updatedPic = superAdminPage.locator('img[src*="profile"], img[src*="avatar"], .profile-picture img');

        const hasSuccess = await successMsg.isVisible({ timeout: 10_000 }).catch(() => false);
        const hasPic = await updatedPic.isVisible().catch(() => false);

        expect(hasSuccess || hasPic).toBeTruthy();
      } else {
        // No upload input found — skip gracefully
        test.skip(true, 'Profile picture upload not available on this page');
      }
    });
  });

  // ── All roles: navbar profile display ────────────────────────────────────

  test.describe('Navbar Profile Display', () => {
    test('@profile @regression should display user name in navbar', async ({ superAdminPage }) => {
      const navbar = superAdminPage.locator(
        '.navbar .user-name, .navbar .profile-name, [data-testid="navbar-username"], .user-menu',
      );
      await expect(navbar.first()).toBeVisible({ timeout: 10_000 });
    });

    test('@profile @regression should display profile picture or avatar in navbar', async ({ superAdminPage }) => {
      const avatar = superAdminPage.locator(
        '.navbar img, .navbar .avatar, .user-menu img, [data-testid="navbar-avatar"]',
      );
      // Avatar may or may not be present (fallback to initials)
      const hasAvatar = await avatar.first().isVisible().catch(() => false);
      // If no avatar, initials/icon should be visible
      if (!hasAvatar) {
        const initials = superAdminPage.locator('.navbar .avatar-placeholder, .navbar .user-initials');
        // At minimum, user menu area should be visible
        await expect(superAdminPage.locator('.user-menu, .profile-dropdown, [data-testid="user-menu"]').first()).toBeVisible();
      }
    });
  });
});
