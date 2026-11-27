import { api, ApiError } from '../../api';
import type { StoredDraft, StoredValue } from './draft';

const ROUTE = '/api/workouts/draft';
const DELAY_MS = 1500;

interface ServerDraft {
  draftId: string;
  content: StoredValue;
  updatedAt: string;
}

export async function readServerDraft(): Promise<StoredValue | null> {
  const draft = await api.get<ServerDraft | undefined>(ROUTE);
  return draft?.content ?? null;
}

export interface ServerDraftSync {
  push(draft: StoredDraft): void;
  /** Sends the waiting change now; `leaving` lets the browser finish it after the tab closes. */
  flush(leaving?: boolean): void;
  cancel(): void;
  discard(draftId: string): void;
  stop(): void;
}

/**
 * Sends the newest draft once changes pause for 1.5 s. A failed request waits for the next change, because
 * the browser keeps its own copy. An empty draft is sent only once its id is known to the server,
 * so opening the logger never replaces a workout started on another device.
 * 409 means the workout was saved elsewhere. A change still waiting when the tab hides goes out with
 * keepalive, because the browser cancels an ordinary request of a closing tab.
 */
export function serverDraftSync(onSavedElsewhere: (draftId: string) => void, serverDraftId?: string): ServerDraftSync {
  let timer: ReturnType<typeof setTimeout> | undefined;
  let pending: StoredDraft | null = null;
  let stopped = false;
  const known = new Set<string>(serverDraftId ? [serverDraftId] : []);

  function flush(leaving = false) {
    clearTimeout(timer);
    timer = undefined;
    const draft = pending;
    pending = null;
    if (!draft || stopped) return;
    known.add(draft.draftId);
    api.put(ROUTE, { draftId: draft.draftId, content: draft }, { keepalive: leaving }).catch((error: unknown) => {
      if (error instanceof ApiError && error.status === 409 && !stopped) onSavedElsewhere(draft.draftId);
    });
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
  };
}
