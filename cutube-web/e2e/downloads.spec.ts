import { expect, test } from "@playwright/test";
import { mockApi } from "./helpers/mock-api";

test.describe("Downloads", () => {
  test("lista downloads na pagina dedicada", async ({ page }) => {
    await mockApi(page);
    await page.goto("/downloads");

    await expect(page.getByRole("heading", { name: "Todos os downloads" })).toBeVisible();
    await expect(page.getByText("https://www.youtube.com/watch?v=xyz987")).toBeVisible();
  });

  test("abre detalhes do download", async ({ page }) => {
    await mockApi(page);
    await page.goto("/downloads");

    await page.getByRole("link", { name: "Ver detalhes" }).first().click();
    await expect(page).toHaveURL(/\/downloads\//);
    await expect(page.getByRole("heading", { name: "Detalhes do download" })).toBeVisible();
  });
});
