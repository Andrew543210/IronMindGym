import type { Ref } from 'react';
import type { CatalogExercise } from '../../../api/types/exercises';
import type { LoggerOptions, PreviousSet } from '../../../api/types/workouts';
import { Glyph } from '../../../components/ui';
import { useApi } from '../../../hooks';
import { useI18n, type Text } from '../../../i18n/i18n';
import { fieldsFrom, type SetFields } from '../draft';

// Undefined while it loads, null when the exercise has no saved set.
function usePreviousSet(exerciseId: number | undefined): { value?: PreviousSet | null; failed?: boolean } | undefined {
  const { data, error, loading } = useApi<PreviousSet | undefined>(
    exerciseId === undefined ? null : `/api/workouts/previous-set?exerciseId=${exerciseId}`);
  if (exerciseId === undefined || loading) return undefined;
  return error ? { failed: true } : { value: data ?? null };
}

interface SetEditorProps {
  options: LoggerOptions;
  exercise: CatalogExercise | undefined;
  fields: SetFields;
  onFields: (fields: SetFields) => void;
  editingIndex: number | null;
  error: Text | null;
  onSave: () => void;
  onCancel: () => void;
  weightRef: Ref<HTMLInputElement>;
  durationRef: Ref<HTMLInputElement>;
  errorRef: Ref<HTMLDivElement>;
}

export function SetEditor({ options, exercise, fields, onFields, editingIndex, error, onSave, onCancel, weightRef, durationRef, errorRef }: SetEditorProps) {
  const { t, show } = useI18n();
  const previous = usePreviousSet(exercise?.exerciseId);
  const type = exercise?.exerciseType ?? 'StrengthTraining';
  const last = previous?.value;

  function previousText(): string {
    if (!exercise) return t('logger.previousChoose');
    if (!previous) return t('logger.previousLoading');
    if (previous.failed) return t('logger.previousFailed');
    if (!last) return t('logger.previousNone');
    const value = type === 'StrengthTraining'
      ? t('logger.previousStrength', { weight: last.weightKg, reps: t('count.reps', { count: last.reps }) })
      : type === 'Cardio'
        ? t('logger.previousCardio', { seconds: last.durationSeconds, effort: last.intensity })
        : t('logger.previousTimed', { seconds: last.durationSeconds, weight: last.weightKg });
    return t('logger.previous', { date: last.localDate, value });
  }

  const setField = (name: keyof SetFields, value: string) => onFields({ ...fields, [name]: value });

  return (
    <>
      <div className="col-12">
        <div className="previous-set">
          <span className="small text-muted" role="status">{previousText()}</span>
          {last && (
            <button type="button" className="btn btn-outline-secondary btn-sm"
              onClick={() => onFields(fieldsFrom(last, options.defaultSetTypeId))}>{t('logger.useValues')}</button>
          )}
        </div>
      </div>
      <div className="col-md-3">
        <label htmlFor="setTypeSelect" className="form-label">{t('logger.setType')}</label>
        <select id="setTypeSelect" className="form-select" value={fields.setTypeId} onChange={(e) => setField('setTypeId', e.target.value)}>
          {options.setTypes.map((s) => <option key={s.id} value={s.id}>{s.name}</option>)}
        </select>
      </div>
      {type !== 'Cardio' && (
        <div className="col-md-3">
          <label htmlFor="weightInput" className="form-label">{t('logger.load')}</label>
          <input id="weightInput" ref={weightRef} className="form-control" type="number" inputMode="decimal" min={0} max={500} step={0.5}
            value={fields.weightKg} onChange={(e) => setField('weightKg', e.target.value)} />
        </div>
      )}
      {type === 'StrengthTraining' && (
        <div className="col-md-3">
          <label htmlFor="repsInput" className="form-label">{t('logger.reps')}</label>
          <input id="repsInput" className="form-control" type="number" inputMode="numeric" min={1} max={500}
            value={fields.reps} onChange={(e) => setField('reps', e.target.value)} />
        </div>
      )}
      {type !== 'StrengthTraining' && (
        <div className="col-md-3">
          <label htmlFor="durationInput" className="form-label">{t('logger.durationSeconds')}</label>
          <input id="durationInput" ref={durationRef} className="form-control" type="number" inputMode="numeric" min={1} max={86400}
            value={fields.durationSeconds} onChange={(e) => setField('durationSeconds', e.target.value)} />
        </div>
      )}
      {type === 'Cardio' && (
        <div className="col-md-3">
          <label htmlFor="intensityInput" className="form-label">{t('logger.effort')}</label>
          <input id="intensityInput" className="form-control" type="number" inputMode="numeric" min={1} max={10}
            value={fields.intensity} onChange={(e) => setField('intensity', e.target.value)} />
        </div>
      )}
      <div className="col-md-3">
        <label htmlFor="restInput" className="form-label">{t('logger.rest')}</label>
        <input id="restInput" className="form-control" type="number" inputMode="numeric" min={0} max={3600}
          value={fields.restSeconds} onChange={(e) => setField('restSeconds', e.target.value)} />
      </div>

      <div className="col-12">
        <div ref={errorRef} className="alert alert-danger" role="alert" hidden={!error}>{error ? show(error) : ''}</div>
        <div className="d-flex gap-2">
          <button type="button" className="btn btn-primary flex-grow-1" onClick={onSave}>
            {editingIndex === null ? <>{t('logger.logSet')} <Glyph>＋</Glyph></> : t('logger.saveSet', { number: editingIndex + 1 })}
          </button>
          {editingIndex !== null && <button type="button" className="btn btn-outline-secondary" onClick={onCancel}>{t('logger.cancelEdit')}</button>}
        </div>
        <details className="small text-muted mt-2">
          <summary>{t('logger.howTo')}</summary>
          <p className="mt-2 mb-0">{t('logger.howToText')}</p>
        </details>
      </div>
    </>
  );
}
