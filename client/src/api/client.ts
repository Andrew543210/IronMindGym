import { translate } from '../i18n/i18n';
import { currentLanguage } from '../i18n/language';

/**
 * Talks to the .NET API. The session is an HttpOnly cookie the browser sends by itself;
 * every change also carries the antiforgery token in the X-CSRF-TOKEN header.
 * Accept-Language asks for names and messages in the interface language.
 */

export class ApiError extends Error {
  readonly status: number;
  readonly messages: string[];

  constructor(status: number, messages: string[]) {
    super(messages[0] ?? translate('errors.requestFailed', { status: String(status) }));
    this.status = status;
    this.messages = messages.length > 0 ? messages : [this.message];
  }
}

export class FormError extends Error {}

type Method = 'GET' | 'POST' | 'PUT' | 'DELETE';

export interface RequestOptions {
  /** Cancels the request once its answer is no longer needed. */
  signal?: AbortSignal;
  /** Lets the request finish after the tab closes; the browser allows it only for a small body. */
  keepalive?: boolean;
}

/** The problem type of a change refused for a missing or expired antiforgery token. */
export const CSRF_PROBLEM = 'urn:ironmind:problem:csrf-token';

interface ProblemDetails {
  type?: string;
  title?: string;
  errors?: Record<string, string[]>;
}

/** Browsers refuse a keepalive request with a larger body instead of sending it the ordinary way. */
const KEEPALIVE_LIMIT_BYTES = 64 * 1024;

let csrfToken: string | null = null;
let onSessionEnded: (() => void) | null = null;

export function whenSessionEnds(handler: () => void) {
  onSessionEnded = handler;
}

export function forgetCsrfToken() {
  csrfToken = null;
}

/**
 * Fetches the token ahead of a change that may have to go out as the tab closes. Such a keepalive request
 * cannot wait for the token: the browser cancels the ordinary request that would fetch it first.
 */
export async function prefetchCsrfToken(): Promise<void> {
  if (csrfToken) return;
  try {
    csrfToken = await fetchCsrfToken();
  } catch {
    // The change fetches the token itself when it goes out.
  }
}

export async function request<T>(method: Method, url: string, body?: unknown, options: RequestOptions = {}): Promise<T> {
  let response: Response;
  if (method === 'GET') {
    response = await send(method, url, body, undefined, options);
  } else {
    csrfToken ??= await fetchCsrfToken();
    response = await send(method, url, body, csrfToken, options);
    // A token that expired, for example after the server's keys changed, is refused before the request
    // does anything, so one retry with a fresh token is safe.
    if (response.status === 400 && (await problemType(response)) === CSRF_PROBLEM) {
      csrfToken = await fetchCsrfToken();
      response = await send(method, url, body, csrfToken, options);
    }
  }
  if (response.status === 401) {
    onSessionEnded?.();
    throw new ApiError(401, [translate('errors.sessionEnded')]);
  }
  if (!response.ok) throw await toApiError(response);
  if (response.status === 204) return undefined as T;
  return (await response.json()) as T;
}

export const api = {
  get: <T>(url: string, options?: RequestOptions) => request<T>('GET', url, undefined, options),
  post: <T = void>(url: string, body?: unknown, options?: RequestOptions) => request<T>('POST', url, body, options),
  put: <T = void>(url: string, body?: unknown, options?: RequestOptions) => request<T>('PUT', url, body, options),
  delete: <T = void>(url: string, options?: RequestOptions) => request<T>('DELETE', url, undefined, options),
};

export function errorMessages(error: unknown): string[] {
  if (error instanceof ApiError) return error.messages;
  if (error instanceof FormError) return [error.message];
  return [translate('errors.generic')];
}

async function fetchCsrfToken(): Promise<string> {
  const response = await send('GET', '/api/session/csrf');
  if (!response.ok) throw await toApiError(response);
  return ((await response.json()) as { token: string }).token;
}

async function send(method: Method, url: string, body?: unknown, token?: string, options: RequestOptions = {}): Promise<Response> {
  const headers: Record<string, string> = { Accept: 'application/json', 'Accept-Language': currentLanguage() };
  if (body !== undefined) headers['Content-Type'] = 'application/json';
  if (token) headers['X-CSRF-TOKEN'] = token;
  const json = body === undefined ? undefined : JSON.stringify(body);
  const keepalive = options.keepalive === true && (json === undefined || new TextEncoder().encode(json).length <= KEEPALIVE_LIMIT_BYTES);
  try {
    return await fetch(url, { method, headers, credentials: 'same-origin', body: json, signal: options.signal, keepalive });
  } catch (failure) {
    if (options.signal?.aborted) throw failure;
    throw new ApiError(0, [translate('errors.offline')]);
  }
}

function isJson(response: Response) {
  return response.headers.get('Content-Type')?.includes('json') ?? false;
}

/** Reads the problem type from a copy, so the body stays readable for the error message. */
async function problemType(response: Response): Promise<string | undefined> {
  if (!isJson(response)) return undefined;
  try {
    return ((await response.clone().json()) as ProblemDetails).type;
  } catch {
    return undefined;
  }
}

async function toApiError(response: Response): Promise<ApiError> {
  if (response.status === 429) return new ApiError(429, [translate('errors.tooMany')]);
  if (response.status === 404) return new ApiError(404, [translate('errors.notFound')]);
  let problem: ProblemDetails = {};
  if (isJson(response)) {
    try {
      problem = (await response.json()) as ProblemDetails;
    } catch {
      // The body is not usable; the status decides the message below.
    }
  }
  const messages = Object.values(problem.errors ?? {}).flat();
  if (messages.length === 0 && response.status < 500 && problem.title) messages.push(problem.title);
  if (messages.length === 0)
    messages.push(translate(response.status >= 500 ? 'errors.server' : 'errors.request'));
  return new ApiError(response.status, messages);
}
