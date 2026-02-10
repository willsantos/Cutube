import { expect, test } from "@playwright/test";
import { mockApi } from "./helpers/mock-api";

test("navega entre dashboard e downloads", async ({ page }) => {
  await mockApi(page);
  await page.goto("/");

  const openMenu = page.getByRole("button", { name: "Abrir menu" });
  if (await openMenu.isVisible()) {
    await openMenu.click();
  }

  await page.getByRole("link", { name: "Downloads" }).click();
  await expect(page).toHaveURL("/downloads");
  await expect(page.getByRole("heading", { name: "Todos os downloads" })).toBeVisible();

  if (await openMenu.isVisible()) {
    await openMenu.click();
  }

  await page.getByRole("link", { name: "Dashboard" }).click();
  await expect(page).toHaveURL("/");
  await expect(page.getByRole("heading", { name: "Dashboard" })).toBeVisible();
});
