import type { NextConfig } from "next";

const nextConfig: NextConfig = {
  reactStrictMode: true,
  async rewrites() {
    const apiBaseUrl =
      process.env.API_INTERNAL_URL?.trim() ||
      process.env.NEXT_PUBLIC_API_URL?.trim() ||
      "http://localhost:5000";

    return [
      {
        source: "/api/:path*",
        destination: `${apiBaseUrl}/api/:path*`,
      },
      {
        source: "/hubs/:path*",
        destination: `${apiBaseUrl}/hubs/:path*`,
      },
      {
        source: "/health",
        destination: `${apiBaseUrl}/health`,
      },
    ];
  },
};

export default nextConfig;
