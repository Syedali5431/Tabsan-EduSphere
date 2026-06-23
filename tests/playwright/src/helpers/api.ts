/**
 * API helpers for direct backend assertions.
 *
 * Use these to verify data state after UI operations (e.g., confirm attendance
 * was saved, results were posted, export endpoints work, etc.)
 */
import { APIRequestContext, expect } from '@playwright/test';

// ── Types ──────────────────────────────────────────────────────────────────

export interface ApiResponse<T = unknown> {
  status: number;
  body: T;
  headers: Record<string, string>;
}

// ── Generic GET ────────────────────────────────────────────────────────────

export async function apiGet<T = unknown>(
  context: APIRequestContext,
  endpoint: string,
  params?: Record<string, string>,
): Promise<ApiResponse<T>> {
  const query = params ? '?' + new URLSearchParams(params).toString() : '';
  const res = await context.get(`${endpoint}${query}`);
  return {
    status: res.status(),
    body: (await res.json().catch(() => ({}))) as T,
    headers: res.headers() as Record<string, string>,
  };
}

// ── Generic POST ───────────────────────────────────────────────────────────

export async function apiPost<T = unknown>(
  context: APIRequestContext,
  endpoint: string,
  data?: unknown,
): Promise<ApiResponse<T>> {
  const res = await context.post(endpoint, { data });
  return {
    status: res.status(),
    body: (await res.json().catch(() => ({}))) as T,
    headers: res.headers() as Record<string, string>,
  };
}

// ── Attendance verification ───────────────────────────────────────────────

export async function apiGetAttendance(
  context: APIRequestContext,
  params: { courseId?: string; semesterId?: string; classId?: string; date?: string },
): Promise<ApiResponse<unknown[]>> {
  return apiGet<unknown[]>(context, '/api/attendance', params as Record<string, string>);
}

// ── Results verification ──────────────────────────────────────────────────

export async function apiGetResults(
  context: APIRequestContext,
  params: { courseId?: string; semesterId?: string; studentId?: string },
): Promise<ApiResponse<unknown[]>> {
  return apiGet<unknown[]>(context, '/api/results', params as Record<string, string>);
}

// ── Students verification ─────────────────────────────────────────────────

export async function apiGetStudents(
  context: APIRequestContext,
  params: { institutionId?: string; departmentId?: string; courseId?: string; semesterId?: string },
): Promise<ApiResponse<unknown[]>> {
  return apiGet<unknown[]>(context, '/api/students', params as Record<string, string>);
}

// ── Certificates verification ─────────────────────────────────────────────

export async function apiGetCertificates(
  context: APIRequestContext,
  params: { studentId?: string; certificateType?: string },
): Promise<ApiResponse<unknown[]>> {
  return apiGet<unknown[]>(context, '/api/certificates', params as Record<string, string>);
}

// ── Payments verification ─────────────────────────────────────────────────

export async function apiGetPayments(
  context: APIRequestContext,
  params: { institutionId?: string; studentId?: string },
): Promise<ApiResponse<unknown[]>> {
  return apiGet<unknown[]>(context, '/api/payments', params as Record<string, string>);
}

// ── Export endpoints ───────────────────────────────────────────────────────

/**
 * Verify an export endpoint returns a file with the expected content type.
 */
export async function assertExportFile(
  context: APIRequestContext,
  endpoint: string,
  expectedContentType: string,
): Promise<void> {
  const res = await context.get(endpoint);
  expect(res.status()).toBe(200);

  const contentType = res.headers()['content-type'] || '';
  expect(contentType).toContain(expectedContentType);
}

/**
 * Verify PDF export returns valid PDF bytes.
 */
export async function assertPdfExport(context: APIRequestContext, endpoint: string): Promise<void> {
  const res = await context.get(endpoint);
  expect(res.status()).toBe(200);
  const contentType = res.headers()['content-type'] || '';
  expect(contentType).toContain('application/pdf');

  const buffer = await res.body();
  // PDF files start with %PDF
  expect(buffer.slice(0, 4).toString()).toBe('%PDF');
}

/**
 * Verify CSV export returns valid CSV text.
 */
export async function assertCsvExport(context: APIRequestContext, endpoint: string): Promise<void> {
  const res = await context.get(endpoint);
  expect(res.status()).toBe(200);
  const contentType = res.headers()['content-type'] || '';
  expect(contentType).toMatch(/text\/csv|application\/csv/);

  const text = await res.text();
  expect(text.length).toBeGreaterThan(0);
}

/**
 * Verify Excel export returns valid .xlsx bytes.
 */
export async function assertExcelExport(context: APIRequestContext, endpoint: string): Promise<void> {
  const res = await context.get(endpoint);
  expect(res.status()).toBe(200);
  const contentType = res.headers()['content-type'] || '';
  expect(contentType).toMatch(/spreadsheet|excel|officedocument/);

  const buffer = await res.body();
  // .xlsx files are ZIP archives starting with PK
  expect(buffer.slice(0, 2).toString()).toBe('PK');
}

// ── API health check ───────────────────────────────────────────────────────

export async function assertApiHealthy(context: APIRequestContext): Promise<void> {
  const res = await context.get('/health');
  expect(res.status()).toBeLessThan(400);
}
