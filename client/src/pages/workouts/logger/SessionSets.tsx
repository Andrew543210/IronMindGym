import type { CatalogExercise } from '../../../api/types/exercises';
import { useI18n } from '../../../i18n/i18n';
import { setCells, type DraftSet } from '../draft';

interface SessionSetsProps {
  sets: DraftSet[];
  catalog: CatalogExercise[];
  setTypeName: (id: number) => string | undefined;
  onEdit: (index: number) => void;
  onRemove: (index: number) => void;
}

/** The sets logged so far; on a phone each row becomes a card that names its cells from the column headers. */
export function SessionSets({ sets, catalog, setTypeName, onEdit, onRemove }: SessionSetsProps) {
  const { t } = useI18n();
  const columns = [t('common.exercise'), t('workout.type'), t('logger.loadOrDuration'), t('logger.repsOrEffort'), t('workout.rest')];

  return (
    <div className="col-12">
      <div className="section-heading">
        <h2 className="h5 mb-0">{t('logger.thisSession')}</h2>
        <span className="small text-muted">{t('logger.logged', { sets: t('count.sets', { count: sets.length }) })}</span>
      </div>
      {sets.length === 0 && <p className="text-muted small">{t('logger.firstSet')}</p>}
      <div className="table-responsive">
        <table className="table table-sm session-sets">
          <thead>
            <tr>
              <th>#</th>
              {columns.map((column) => <th key={column}>{column}</th>)}
              <th><span className="visually-hidden">{t('logger.setActions')}</span></th>
            </tr>
          </thead>
          <tbody>
            {sets.map((set, index) => (
              <tr key={index}>
                <td>{index + 1}</td>
                {setCells(set, catalog.find((e) => e.exerciseId === set.exerciseId), t, setTypeName(set.setTypeId))
                  .map((cell, i) => <td key={i} data-label={columns[i]}>{cell}</td>)}
                <td className="text-nowrap">
                  <button type="button" className="btn btn-sm btn-outline-secondary" aria-label={t('logger.editSet', { number: index + 1 })}
                    onClick={() => onEdit(index)}>{t('common.edit')}</button>{' '}
                  <button type="button" className="btn btn-sm btn-outline-danger" aria-label={t('logger.removeSet', { number: index + 1 })}
                    onClick={() => onRemove(index)}>×</button>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  );
}
