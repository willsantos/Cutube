import { expect, test } from "@playwright/test";
import { mockApi } from "./helpers/mock-api";

test("alterna entre tema claro e escuro", async ({ page }) => {
  await mockApi(page);
  await page.goto("/");

  const html = page.locator("html");
  const toggle = page.getByRole("button", { name: /Mudar para modo/ });

  await toggle.click();
  await expect(html).toHaveClass(/dark/);

  await page.reload();
  await expect(html).toHaveClass(/dark/);
});
