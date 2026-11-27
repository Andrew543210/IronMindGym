import { useEffect, useRef, useState } from 'react';
import { useNow, usePageTitle } from '../../../hooks';
import { useI18n } from '../../../i18n/i18n';
import { clockText } from '../draft';

// Both clocks are derived from timestamps, so a throttled background tab shows the right values when it returns.

export function SessionClock({ startedAt }: { startedAt: number }) {
  const now = useNow(500);
  return <strong className="metric">{clockText(Math.max(0, Math.floor((now - startedAt) / 1000)))}</strong>;
}

interface RestTimerProps {
  /** When the rest ends; 0 before the first rest. Render the timer with it as the key, so each rest starts a new count. */
  timerEnd: number;
  onStart: () => void;
  onSkip: () => void;
}

/** Counts the rest down in the page and the tab title, and buzzes once when a rest it saw running is over. */
export function RestTimer({ timerEnd, onStart, onSkip }: RestTimerProps) {
  const { t } = useI18n();
  const now = useNow(500);
  // A rest that was over, skipped or zero when it appeared ends without a buzz.
  const [running] = useState(() => timerEnd > Date.now());
  const alerted = useRef(false);

  const remaining = Math.max(0, Math.ceil((timerEnd - now) / 1000));
  const resting = timerEnd > 0 && remaining > 0;
  const done = timerEnd > 0 && !resting;
  usePageTitle(resting ? t('logger.restTitle', { time: clockText(remaining) }) : t('workouts.title'));

  useEffect(() => {
    if (!done || !running || alerted.current) return;
    alerted.current = true;
    navigator.vibrate?.([200, 100, 200]);
  }, [done, running]);

  return (
    <div className="col-12">
      <div className={`workout-toolbar rest-timer d-flex flex-wrap gap-2 gap-sm-3 align-items-center justify-content-between${done ? ' rest-done' : ''}`}>
        <span>{t('logger.restTimer')} <strong className="metric rest-timer-value">{clockText(remaining)}</strong></span>
        <div>
          <button type="button" className="btn btn-outline-secondary btn-sm" onClick={onStart}>{t('logger.startTimer')}</button>{' '}
          <button type="button" className="btn btn-outline-warning btn-sm" onClick={onSkip}>{t('logger.skipRest')}</button>
        </div>
      </div>
      <div className="mt-2 rest-ready" role="status">{done ? t('logger.restDone') : ''}</div>
    </div>
  );
}
