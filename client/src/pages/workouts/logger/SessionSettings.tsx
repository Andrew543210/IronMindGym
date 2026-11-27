import type { LoggerOptions } from '../../../api/types/workouts';
import { useNow } from '../../../hooks';
import { useI18n } from '../../../i18n/i18n';
import { elapsedMinutes, type DraftSettings, type StoredDraft } from '../draft';
import { dayIds } from './useWorkoutDraft';

interface SessionSettingsProps {
  options: LoggerOptions;
  draft: StoredDraft;
  onChange: (next: StoredDraft) => void;
}

/** The split, its day and the duration, folded away: most sessions keep the defaults. */
export function SessionSettings({ options, draft, onChange }: SessionSettingsProps) {
  const { t } = useI18n();
  const now = useNow(1000);
  const elapsed = elapsedMinutes(draft.startedAt, now);
  const splitDays = options.splitTypes.find((s) => String(s.id) === draft.settings.splitTypeId)?.days ?? [];
  const dayOptions = splitDays.length > 0
    ? splitDays.map((day) => ({ value: String(day.id), label: day.name }))
    : [{ value: '', label: t('logger.trainingDay') }];

  function setSetting(name: keyof DraftSettings, value: string) {
    const settings = { ...draft.settings, [name]: value };
    if (name === 'splitTypeId') settings.splitDayId = dayIds(options, value)[0];
    onChange({ ...draft, settings, autoDuration: name === 'durationMinutes' ? false : draft.autoDuration });
  }

  function setAutoDuration(on: boolean) {
    const settings = on ? draft.settings : { ...draft.settings, durationMinutes: String(elapsedMinutes(draft.startedAt, Date.now())) };
    onChange({ ...draft, autoDuration: on, settings });
  }

  return (
    <div className="col-12">
      <div className="card">
        <details>
          <summary className="card-header">{t('logger.settings')}{options.activeSplitTypeName ? ` · ${options.activeSplitTypeName}` : ''}</summary>
          <div className="card-body row g-3">
            <div className="col-md-4">
              <label htmlFor="splitType" className="form-label">{t('setup.splitType')}</label>
              <select id="splitType" className="form-select" value={draft.settings.splitTypeId}
                disabled={options.activeSplitTypeId !== null} onChange={(e) => setSetting('splitTypeId', e.target.value)}>
                {options.splitTypes.map((s) => <option key={s.id} value={s.id}>{s.name}</option>)}
              </select>
            </div>
            <div className="col-md-4">
              <label htmlFor="splitDay" className="form-label">{t('logger.splitDay')}</label>
              <select id="splitDay" className="form-select" value={draft.settings.splitDayId}
                onChange={(e) => setSetting('splitDayId', e.target.value)}>
                {dayOptions.map((day) => <option key={day.value} value={day.value}>{day.label}</option>)}
              </select>
            </div>
            <div className="col-md-4">
              <label htmlFor="durationMinutes" className="form-label">{t('logger.durationMinutes')}</label>
              <input id="durationMinutes" type="number" className="form-control" min={1} max={600} inputMode="numeric"
                value={draft.autoDuration ? String(elapsed) : draft.settings.durationMinutes}
                onChange={(e) => setSetting('durationMinutes', e.target.value)} />
              <label className="small mt-2">
                <input type="checkbox" checked={draft.autoDuration} onChange={(e) => setAutoDuration(e.target.checked)} /> {t('logger.useElapsed')}
              </label>
            </div>
          </div>
        </details>
      </div>
    </div>
  );
}
