import { expect, test } from "@playwright/test";
import { mockApi } from "./helpers/mock-api";

test.describe("Dashboard", () => {
  test("carrega formulario e lista", async ({ page }) => {
    await mockApi(page);
    await page.goto("/");

    await expect(page.getByRole("heading", { name: "Dashboard" })).toBeVisible();
    await expect(page.getByLabel("URL do video")).toBeVisible();
    await expect(page.getByRole("heading", { name: "Downloads recentes" })).toBeVisible();
    await expect(page.getByText("https://www.youtube.com/watch?v=abc123")).toBeVisible();
  });

  test("submete form de download", async ({ page }) => {
    await mockApi(page);
    await page.goto("/");

    await page.getByLabel("URL do video").fill("https://www.youtube.com/watch?v=abc123");
    await page.getByRole("button", { name: "Iniciar download" }).click();

    await expect(page.getByText("Downloads recentes")).toBeVisible();
  });
});
