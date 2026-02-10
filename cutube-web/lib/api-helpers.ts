import type { ApiError } from "@/types";
import { API_BASE_URL } from "./constants";

function extractErrorMessage(error: ApiError, fallback: string): string {
  if (error.detail) return error.detail;
  if (error.title) return error.title;

  if (error.errors) {
    const firstGroup = Object.values(error.errors)[0];
    if (firstGroup?.length) return firstGroup[0];
  }

  return fallback;
}

export async function fetchApi<T>(endpoint: string, options?: RequestInit): Promise<T> {
  const url = `${API_BASE_URL}${endpoint}`;

  const response = await fetch(url, {
    ...options,
    headers: {
      "Content-Type": "application/json",
      ...options?.headers,
    },
  });

  if (!response.ok) {
    let parsedError: ApiError | undefined;

    try {
      parsedError = (await response.json()) as ApiError;
    } catch {
      parsedError = undefined;
    }

    throw new Error(extractErrorMessage(parsedError ?? {}, response.statusText));
  }

  if (response.status === 204) return undefined as T;

  return (await response.json()) as T;
}

export function buildQueryString(
  params: Record<string, string | number | boolean | undefined>,
): string {
  const query = new URLSearchParams();

  for (const [key, value] of Object.entries(params)) {
    if (value !== undefined) {
      query.append(key, String(value));
    }
  }

  const queryString = query.toString();
  return queryString ? `?${queryString}` : "";
}
