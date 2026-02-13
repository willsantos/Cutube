import { expect, test } from "@playwright/test";
import { mockApi } from "./helpers/mock-api";

test.describe("Responsividade", () => {
  test("layout mobile e desktop", async ({ page }) => {
    await mockApi(page);
    await page.setViewportSize({ width: 320, height: 640 });
    await page.goto("/");

    await expect(page.getByRole("button", { name: "Abrir menu" })).toBeVisible();

    await page.setViewportSize({ width: 1280, height: 800 });
    await expect(page.getByRole("link", { name: "Dashboard" })).toBeVisible();
    await expect(page.getByRole("link", { name: "Downloads" })).toBeVisible();
  });
});
