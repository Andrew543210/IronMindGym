import { translateText, type Text } from '../i18n/i18n';
import { currentLanguage } from '../i18n/language';

// The client's own message is kept as a text and worded when shown, so it follows a change of language.
export class ApiError extends Error {
  readonly status: number;
  private readonly worded: string[];
  private readonly text: Text;

  constructor(status: number, messages: string[] | Text) {
    const worded = Array.isArray(messages) ? messages : [];
    const text: Text = Array.isArray(messages) ? { key: 'errors.requestFailed', params: { status: String(status) } } : messages;
    super(worded[0] ?? translateText(text));
    this.status = status;
    this.worded = worded;
    this.text = text;
  }

  get messages(): string[] {
    return this.worded.length > 0 ? this.worded : [translateText(this.text)];
  }
}

export class FormError extends Error {
  readonly text: Text;

  constructor(text: Text) {
    super(translateText(text));
    this.text = text;
  }
}

type Method = 'GET' | 'POST' | 'PUT' | 'DELETE';

export interface RequestOptions {
  signal?: AbortSignal;
  keepalive?: boolean;
}

export const CSRF_PROBLEM = 'urn:ironmind:problem:csrf-token';

interface ProblemDetails {
  type?: string;
  title?: string;
  errors?: Record<string, string[]>;
}

// Browsers refuse a keepalive request with a larger body instead of sending it the ordinary way.
const KEEPALIVE_LIMIT_BYTES = 64 * 1024;

let csrfToken: string | null = null;
let onSessionEnded: (() => void) | null = null;

export function whenSessionEnds(handler: () => void) {
  onSessionEnded = handler;
}

export function forgetCsrfToken() {
  csrfToken = null;
}

// A keepalive request cannot wait for the token: as the tab closes, the browser cancels the request that would fetch it.
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
    // An expired token is refused before the request does anything, so one retry with a fresh token is safe.
    if (response.status === 400 && (await problemType(response)) === CSRF_PROBLEM) {
      csrfToken = await fetchCsrfToken();
      response = await send(method, url, body, csrfToken, options);
    }
  }
  if (response.status === 401) {
    onSessionEnded?.();
    throw new ApiError(401, { key: 'errors.sessionEnded' });
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
  if (error instanceof FormError) return [translateText(error.text)];
  return [translateText({ key: 'errors.generic' })];
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
    throw new ApiError(0, { key: 'errors.offline' });
  }
}

function isJson(response: Response) {
  return response.headers.get('Content-Type')?.includes('json') ?? false;
}

// Read from a copy, so the body stays readable for the error message.
async function problemType(response: Response): Promise<string | undefined> {
  if (!isJson(response)) return undefined;
  try {
    return ((await response.clone().json()) as ProblemDetails).type;
  } catch {
    return undefined;
  }
}

async function toApiError(response: Response): Promise<ApiError> {
  if (response.status === 429) return new ApiError(429, { key: 'errors.tooMany' });
  if (response.status === 404) return new ApiError(404, { key: 'errors.notFound' });
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
  if (messages.length === 0) return new ApiError(response.status, { key: response.status >= 500 ? 'errors.server' : 'errors.request' });
  return new ApiError(response.status, messages);
}
