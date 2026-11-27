import type { ReactNode } from 'react';
import type { ProgressEntry } from '../../api/types/progress';
import { formatNumber, formatSigned } from '../../format';
import { useI18n } from '../../i18n/i18n';

function byMuscleGroup(entries: ProgressEntry[]): [string, ProgressEntry[]][] {
  const groups = new Map<string, ProgressEntry[]>();
  for (const entry of entries) groups.set(entry.muscleGroup, [...(groups.get(entry.muscleGroup) ?? []), entry]);
  return [...groups];
}

function Result({ entry, at }: { entry: ProgressEntry; at: 'initial' | 'current' }) {
  const { t } = useI18n();
  const weight = at === 'initial' ? entry.initialWeightKg : entry.currentWeightKg;
  const reps = at === 'initial' ? entry.initialReps : entry.currentReps;
  const duration = at === 'initial' ? entry.initialDurationSeconds : entry.currentDurationSeconds;
  const intensity = at === 'initial' ? entry.initialIntensity : entry.currentIntensity;
  if (entry.exerciseType === 'StrengthTraining') {
    return (
      <>
        <p className="small mb-1"><strong>{formatNumber(weight)}</strong> {t('unit.kgShort')}</p>
        <p className="small mb-0"><strong>{reps}</strong> {t('word.reps', { count: reps })}</p>
      </>
    );
  }
  return (
    <>
      <p className="small mb-1"><strong>{duration}</strong> {t('unit.secShort')}</p>
      <p className="small mb-0"><strong>{entry.exerciseType === 'Cardio' ? `${intensity}/10` : t('unit.kg', { value: weight })}</strong></p>
    </>
  );
}

function Change({ entry }: { entry: ProgressEntry }) {
  const { t } = useI18n();
  if (!entry.hasCurrentResult) return <p className="small text-muted">{t('progress.addCurrent')}</p>;
  if (entry.exerciseType === 'StrengthTraining') {
    return (
      <>
        <p className="small mb-1">{t('unit.kg', { value: formatSigned(entry.weightProgressKg) })}</p>
        <p className="small mb-0">{t('progress.repsChange', { value: formatSigned(entry.repsProgressCount, 0) })}</p>
      </>
    );
  }
  return (
    <>
      <p className="small mb-1">{t('unit.sec', { value: formatSigned(entry.cardioDurationProgressSeconds, 0) })}</p>
      <p className="small mb-0">
        {entry.exerciseType === 'Cardio'
          ? t('progress.effortChange', { value: formatSigned(entry.cardioIntensityProgressCount, 0) })
          : t('unit.kg', { value: formatSigned(entry.weightProgressKg) })}
      </p>
    </>
  );
}

function ProgressCard({ entry, children }: { entry: ProgressEntry; children?: ReactNode }) {
  const { t } = useI18n();
  return (
    <div className="col-md-6 col-lg-4">
      <div className="card h-100 border border-light d-flex flex-column">
        <div className="card-header bg-dark text-white">
          <h3 className="fs-6 fw-bold mb-0">{entry.exerciseName}</h3>
          <small className="text-muted">{entry.muscleSubGroup}</small>
        </div>
        <div className="card-body d-flex flex-column flex-grow-1">
          <div className="row g-2 mb-3 flex-grow-1">
            <div className="col-4"><div className="border-end ps-0 pe-2">
              <h4 className="h6 text-muted small fw-bold text-uppercase">{t('progress.initial')}</h4>
              <Result entry={entry} at="initial" />
            </div></div>
            <div className="col-4"><div className="text-center border-end ps-1 pe-1">
              <h4 className="h6 text-muted small fw-bold text-uppercase">{t('progress.change')}</h4>
              <Change entry={entry} />
            </div></div>
            <div className="col-4"><div className="ps-2 pe-0">
              <h4 className="h6 text-info small fw-bold text-uppercase">{t('progress.current')}</h4>
              {entry.hasCurrentResult ? <Result entry={entry} at="current" /> : <p className="small text-muted">{t('progress.notRecorded')}</p>}
            </div></div>
          </div>
          {children}
        </div>
      </div>
    </div>
  );
}

export function ProgressGroups({ entries, controls }: { entries: ProgressEntry[]; controls?: (entry: ProgressEntry) => ReactNode }) {
  return (
    <div className="row">
      {byMuscleGroup(entries).map(([group, items]) => (
        <div className="col-md-12 mb-4" key={group}>
          <h2 className="h4">{group}</h2>
          <div className="row g-3">
            {items.map((entry) => <ProgressCard key={entry.exerciseId} entry={entry}>{controls?.(entry)}</ProgressCard>)}
          </div>
        </div>
      ))}
    </div>
  );
}
