import { api, ApiError } from '../../api/client';
import type { StoredDraft, StoredValue } from './draft';

export const SERVER_DRAFT_ROUTE = '/api/workouts/draft';
const ROUTE = SERVER_DRAFT_ROUTE;
const DELAY_MS = 1500;

export interface ServerDraft {
  draftId: string;
  content: StoredValue;
  updatedAt: string;
}

// Read once as the logger opens, not through useApi: a change of language reloads what useApi holds.
export async function readServerDraft(): Promise<StoredValue | null> {
  const draft = await api.get<ServerDraft | undefined>(ROUTE);
  return draft?.content ?? null;
}

export interface ServerDraftSync {
  push(draft: StoredDraft): void;
  // `leaving` lets the browser finish the request after the tab closes.
  flush(leaving?: boolean): void;
  cancel(): void;
  discard(draftId: string): void;
  stop(): void;
  settle(): Promise<void>;
  whenSavedElsewhere(listener: (draftId: string) => void): () => void;
}

const open = new Set<ServerDraftSync>();

export function holdForSignOut(sync: ServerDraftSync): () => void {
  open.add(sync);
  return () => {
    open.delete(sync);
  };
}

// The sign-out ends the session these requests need.
export async function settleServerDrafts(): Promise<void> {
  await Promise.all([...open].map((sync) => sync.settle()));
}

// Sends the newest draft once changes pause for 1.5 s; a failure waits for the next change, as the browser keeps
// its copy. An empty draft goes out only once the server knows its id, so it never replaces a workout started
// on another device. A change still waiting when the tab hides goes out with keepalive.
export function serverDraftSync(serverDraftId?: string, onSent?: (draft: StoredDraft) => void): ServerDraftSync {
  let onSavedElsewhere: ((draftId: string) => void) | undefined;
  let timer: ReturnType<typeof setTimeout> | undefined;
  let pending: StoredDraft | null = null;
  let sending: Promise<void> = Promise.resolve();
  let stopped = false;
  const known = new Set<string>(serverDraftId ? [serverDraftId] : []);

  function flush(leaving = false) {
    clearTimeout(timer);
    timer = undefined;
    const draft = pending;
    pending = null;
    if (!draft || stopped) return;
    known.add(draft.draftId);
    sending = api.put(ROUTE, { draftId: draft.draftId, content: draft }, { keepalive: leaving }).then(
      () => onSent?.(draft),
      (error: unknown) => {
        if (error instanceof ApiError && error.status === 409 && !stopped) onSavedElsewhere?.(draft.draftId);
      },
    );
  }

  return {
    push(draft) {
      if (stopped || (draft.sets.length === 0 && !known.has(draft.draftId))) return;
      pending = draft;
      clearTimeout(timer);
      timer = setTimeout(flush, DELAY_MS);
    },
    flush,
    cancel() {
      clearTimeout(timer);
      pending = null;
    },
    discard(draftId) {
      clearTimeout(timer);
      pending = null;
      known.delete(draftId);
      api.delete(`${ROUTE}?draftId=${encodeURIComponent(draftId)}`).catch(() => undefined);
    },
    stop() {
      stopped = true;
      clearTimeout(timer);
      pending = null;
    },
    async settle() {
      flush();
      await sending;
    },
    whenSavedElsewhere(listener) {
      onSavedElsewhere = listener;
      return () => {
        if (onSavedElsewhere === listener) onSavedElsewhere = undefined;
      };
    },
  };
}
