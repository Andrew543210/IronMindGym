import { beforeEach, describe, expect, it, vi } from 'vitest';
import { freshDraft, type DraftSettings, type StoredDraft } from './draft';
import { serverDraftSync } from './serverDraft';

const settings: DraftSettings = { splitTypeId: '2', splitDayId: '3', durationMinutes: '60', reviewRating: '4', reviewComment: '' };
const set = { exerciseId: 7, setTypeId: 1, setTypeName: 'Working', weightKg: 60, reps: 5, durationSeconds: 0, intensity: 0, restSeconds: 90 };

function respond(status: number, body?: unknown) {
  return new Response(body === undefined ? null : JSON.stringify(body), { status, headers: { 'Content-Type': 'application/json' } });
}

let fetchMock: ReturnType<typeof vi.fn>;
let putStatus = 204;

function sent(method: string) {
  return fetchMock.mock.calls.filter(([, init]) => (init as RequestInit).method === method) as [string, RequestInit][];
}

function withSets(count: number): StoredDraft {
  return { ...freshDraft(settings), sets: Array.from({ length: count }, () => set) };
}

beforeEach(() => {
  vi.useFakeTimers();
  putStatus = 204;
  fetchMock = vi.fn(async (url: string) => (url === '/api/session/csrf' ? respond(200, { token: 'token' }) : respond(putStatus)));
  vi.stubGlobal('fetch', fetchMock);
});

describe('server copy of the draft', () => {
  it('sends only the last of quick changes', async () => {
    const sync = serverDraftSync(() => {});
    const draft = withSets(1);

    sync.push(draft);
    sync.push({ ...draft, sets: [set, set] });
    await vi.advanceTimersByTimeAsync(1000);
    expect(sent('PUT')).toHaveLength(0);
    await vi.advanceTimersByTimeAsync(600);

    await vi.waitFor(() => expect(sent('PUT')).toHaveLength(1));
    const [url, init] = sent('PUT')[0];
    expect(url).toBe('/api/workouts/draft');
    const body = JSON.parse(String(init.body));
    expect(body.draftId).toBe(draft.draftId);
    expect(body.content.sets).toHaveLength(2);
  });

  it('does not replace a workout from another device with an empty one', async () => {
    serverDraftSync(() => {}).push(freshDraft(settings));
    await vi.advanceTimersByTimeAsync(2000);
    expect(fetchMock).not.toHaveBeenCalled();
  });

  it('sends an emptied draft the server already has', async () => {
    const draft = freshDraft(settings);
    serverDraftSync(() => {}, draft.draftId).push(draft);
    await vi.advanceTimersByTimeAsync(1500);
    await vi.waitFor(() => expect(sent('PUT')).toHaveLength(1));
  });

  it('lets the last change finish after the tab closes', async () => {
    const sync = serverDraftSync(() => {});
    sync.push(withSets(1));
    sync.flush();
    await vi.waitFor(() => expect(sent('PUT')).toHaveLength(1));
    sync.push(withSets(2));
    sync.flush(true);
    await vi.waitFor(() => expect(sent('PUT')).toHaveLength(2));

    expect(sent('PUT').map(([, init]) => init.keepalive)).toEqual([false, true]);
  });

  it('reports a workout already saved elsewhere', async () => {
    putStatus = 409;
    const savedElsewhere = vi.fn();
    const sync = serverDraftSync(savedElsewhere);
    const draft = withSets(1);

    sync.push(draft);
    sync.flush();

    await vi.waitFor(() => expect(savedElsewhere).toHaveBeenCalledWith(draft.draftId));
  });

  it('removes a discarded workout and drops its pending change', async () => {
    const sync = serverDraftSync(() => {});
    const draft = withSets(1);

    sync.push(draft);
    sync.discard(draft.draftId);
    await vi.advanceTimersByTimeAsync(2000);

    await vi.waitFor(() => expect(sent('DELETE')).toHaveLength(1));
    expect(sent('DELETE')[0][0]).toBe(`/api/workouts/draft?draftId=${draft.draftId}`);
    expect(sent('PUT')).toHaveLength(0);
  });

  it('sends nothing after the workout is saved', async () => {
    const sync = serverDraftSync(() => {});
    sync.push(withSets(1));
    sync.stop();
    sync.flush();
    await vi.advanceTimersByTimeAsync(2000);
    expect(fetchMock).not.toHaveBeenCalled();
  });
});
