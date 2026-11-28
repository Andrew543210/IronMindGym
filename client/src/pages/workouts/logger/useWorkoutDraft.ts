import { useEffect, useMemo, useRef, useState } from 'react';
import type { LoggerOptions } from '../../../api/types/workouts';
import type { Text } from '../../../i18n/i18n';
import { applyStored, draftStore, freshDraft, isDraftId, newerDraft, restoreDraft, type DraftSettings, type StoredDraft, type StoredValue } from '../draft';
import { holdForSignOut, serverDraftSync } from '../serverDraft';

/** The day ids of a split; a split without days logs a general training day, kept as an empty id. */
export function dayIds(options: LoggerOptions, splitTypeId: string): [string, ...string[]] {
  const [first, ...rest] = (options.splitTypes.find((s) => String(s.id) === splitTypeId)?.days ?? []).map((day) => String(day.id));
  return first === undefined ? [''] : [first, ...rest];
}

function withValidSplit(draft: StoredDraft, options: LoggerOptions): StoredDraft {
  const split = options.splitTypes.find((s) => String(s.id) === draft.settings.splitTypeId) ?? options.splitTypes[0];
  const splitTypeId = split ? String(split.id) : '';
  const days = dayIds(options, splitTypeId);
  const day = days.includes(draft.settings.splitDayId) ? draft.settings.splitDayId : days[0];
  return { ...draft, settings: { ...draft.settings, splitTypeId: splitTypeId, splitDayId: day } };
}

function storageWorks(): boolean {
  try {
    localStorage.setItem('ironmind-storage-check', '1');
    localStorage.removeItem('ironmind-storage-check');
    return true;
  } catch {
    return false;
  }
}

function emptied(draft: StoredDraft): StoredDraft {
  return { ...freshDraft(draft.settings), autoDuration: draft.autoDuration };
}

interface WorkoutDraftOptions {
  options: LoggerOptions;
  userId: number;
  /** The draft the server kept, read before the logger opened. */
  serverCopy: StoredValue | null;
  /** False when the server could not be asked, so the device copy is not sent over a draft the server may have. */
  syncWithServer: boolean;
  /** Called when another tab or device replaced the draft, so the logger drops what it was editing. */
  onReplaced: () => void;
}

/**
 * The unfinished workout: restored from this device or the server, kept on the device after every change,
 * sent to the server, followed across tabs and guarded against closing the page.
 */
export function useWorkoutDraft({ options, userId, serverCopy, syncWithServer, onReplaced }: WorkoutDraftOptions) {
  const store = useMemo(() => draftStore(userId), [userId]);
  const defaults = useMemo<DraftSettings>(() => {
    const splitTypeId = String(options.activeSplitTypeId ?? options.splitTypes[0]?.id ?? '');
    return { splitTypeId: splitTypeId, splitDayId: dayIds(options, splitTypeId)[0], durationMinutes: '60', reviewRating: '4', reviewComment: '' };
  }, [options]);

  const [draft, setDraft] = useState(() => withValidSplit(restoreDraft(newerDraft(store.read(), serverCopy), defaults), options));
  // The status is kept as a text key, so it follows a change of the interface language.
  const [status, setStatus] = useState<Text>(() => (draft.sets.length
    ? { key: 'logger.restored', params: { count: draft.sets.length } }
    : { key: 'logger.draftSaved' }));
  const [canStore] = useState(storageWorks);

  const saved = useRef(false);
  const latest = useRef(draft);
  const changedElsewhere = useRef(false);
  const replaced = useRef(onReplaced);
  const [sync] = useState(() => serverDraftSync(
    isDraftId(serverCopy?.draftId) ? serverCopy.draftId : undefined,
    (sent) => store.markSent(sent),
  ));

  useEffect(() => {
    latest.current = draft;
  }, [draft]);

  function update(next: StoredDraft, message: Text = { key: 'logger.draftSaved' }) {
    setDraft({ ...next, savedAt: Date.now() });
    setStatus(message);
  }

  useEffect(() => {
    if (saved.current) return;
    if (changedElsewhere.current) {
      changedElsewhere.current = false;
      return;
    }
    store.write(draft);
    if (syncWithServer) sync.push(draft);
  }, [draft, store, sync, syncWithServer]);

  useEffect(() => {
    const leave = () => sync.flush(true);
    const onHidden = () => {
      if (document.hidden) leave();
    };
    document.addEventListener('visibilitychange', onHidden);
    window.addEventListener('pagehide', leave);
    const release = holdForSignOut(sync);
    return () => {
      document.removeEventListener('visibilitychange', onHidden);
      window.removeEventListener('pagehide', leave);
      sync.flush();
      release();
    };
  }, [sync]);

  useEffect(() => {
    replaced.current = onReplaced;
  });

  useEffect(() => sync.whenSavedElsewhere((draftId) => {
    if (saved.current || draftId !== draft.draftId) return;
    store.clear();
    onReplaced();
    update(emptied(draft), { key: 'logger.savedElsewhere' });
  }));

  useEffect(() => {
    function onStorage(event: StorageEvent) {
      if (event.key !== store.key) return;
      const stored = store.read();
      changedElsewhere.current = true;
      replaced.current();
      setDraft((current) => (stored ? withValidSplit(applyStored(stored, current), options) : emptied(current)));
      setStatus({ key: 'logger.updatedElsewhere' });
    }
    window.addEventListener('storage', onStorage);
    return () => window.removeEventListener('storage', onStorage);
  }, [store, options]);

  useEffect(() => {
    if (draft.sets.length === 0) return;
    function onBeforeUnload(event: BeforeUnloadEvent) {
      if (saved.current) return;
      event.preventDefault();
      event.returnValue = '';
    }
    window.addEventListener('beforeunload', onBeforeUnload);
    return () => window.removeEventListener('beforeunload', onBeforeUnload);
  }, [draft.sets.length]);

  /** Starts an empty workout with the same settings; the server forgets the old draft. */
  function discard() {
    store.clear();
    sync.discard(draft.draftId);
    update(emptied(draft), { key: 'logger.newWorkout' });
  }

  /** Sends the finished workout; while it is on its way the draft is not synced, and a failure keeps it. */
  async function submit<T>(send: () => Promise<T>): Promise<T> {
    sync.cancel();
    let result: T;
    try {
      result = await send();
    } catch (failure) {
      // Another tab may have changed the workout meanwhile; the server gets the newest copy.
      if (syncWithServer) sync.push(latest.current);
      throw failure;
    }
    saved.current = true;
    sync.stop();
    store.clear();
    return result;
  }

  return { draft, status, canStore, update, discard, submit };
}
