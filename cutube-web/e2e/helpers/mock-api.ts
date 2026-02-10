import type { Page } from "@playwright/test";

const downloadsResponse = {
  downloads: [
    {
      downloadId: "dl-123",
      url: "https://www.youtube.com/watch?v=abc123",
      status: "downloading",
      progress: 42,
      createdAt: "2026-02-10T14:30:00Z",
    },
    {
      downloadId: "dl-456",
      url: "https://www.youtube.com/watch?v=xyz987",
      status: "completed",
      progress: 100,
      filePath: "/downloads/video.mp4",
      createdAt: "2026-02-10T12:00:00Z",
      completedAt: "2026-02-10T12:04:00Z",
    },
  ],
  totalCount: 2,
};

const detailResponse = {
  downloadId: "dl-123",
  url: "https://www.youtube.com/watch?v=abc123",
  status: "downloading",
  progress: 42,
  speed: 1_500_000,
  eta: "00:02:00",
  downloadedBytes: 60_000_000,
  totalBytes: 140_000_000,
  filePath: null,
  errorMessage: null,
  createdAt: "2026-02-10T14:30:00Z",
  completedAt: null,
};

export async function mockApi(page: Page) {
  await page.route("**/api/downloads*", async (route, request) => {
    if (request.url().includes("/api/downloads/dl-123")) {
      await route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify(detailResponse),
      });
      return;
    }

    if (request.method() === "POST") {
      await route.fulfill({
        status: 202,
        contentType: "application/json",
        body: JSON.stringify({
          downloadId: "dl-new",
          status: "queued",
          message: "Download enqueued",
        }),
      });
      return;
    }

    if (request.method() === "DELETE") {
      await route.fulfill({
        status: 204,
      });
      return;
    }

    await route.fulfill({
      status: 200,
      contentType: "application/json",
      body: JSON.stringify(downloadsResponse),
    });
  });

  await page.route("**/api/videos/info**", async (route) => {
    await route.fulfill({
      status: 200,
      contentType: "application/json",
      body: JSON.stringify({
        url: "https://www.youtube.com/watch?v=abc123",
        title: "Video de teste",
        uploader: "Canal teste",
        duration: "00:10:00",
        thumbnailUrl: "https://example.com/thumb.jpg",
        uploadDate: "2026-02-10T00:00:00Z",
      }),
    });
  });
}
