import AxeBuilder from "@axe-core/playwright";
import { expect, test } from "@playwright/test";
import { mockApi } from "./helpers/mock-api";

test("pagina inicial sem violacoes axe", async ({ page }) => {
  await mockApi(page);
  await page.goto("/");

  const accessibilityScanResults = await new AxeBuilder({ page }).analyze();
  expect(accessibilityScanResults.violations).toEqual([]);
});
