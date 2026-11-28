import { useState, type KeyboardEvent } from 'react';
import type { CatalogExercise } from '../../../api/types/exercises';
import { useI18n } from '../../../i18n/i18n';

const MAX_MATCHES = 8;

export function ExerciseSearch({ catalog, onPick }: { catalog: CatalogExercise[]; onPick: (exercise: CatalogExercise) => void }) {
  const { t, language } = useI18n();
  const [query, setQuery] = useState('');
  const search = query.trim().toLocaleLowerCase(language);
  const matches = search
    ? catalog.filter((e) => e.exerciseName.toLocaleLowerCase(language).includes(search) || e.englishName.toLowerCase().includes(search))
      .slice(0, MAX_MATCHES)
    : [];

  function pick(exercise: CatalogExercise) {
    setQuery('');
    onPick(exercise);
  }

  function onKeyDown(event: KeyboardEvent<HTMLInputElement>) {
    if (event.key !== 'Enter') return;
    event.preventDefault();
    if (matches[0]) pick(matches[0]);
  }

  return (
    <div className="col-12">
      <label htmlFor="exerciseSearch" className="form-label">{t('logger.find')}</label>
      <input type="search" id="exerciseSearch" className="form-control" placeholder={t('logger.searchPlaceholder')}
        autoComplete="off" value={query} onChange={(e) => setQuery(e.target.value)} onKeyDown={onKeyDown} />
      <div className="exercise-search-results" aria-live="polite">
        {search && matches.length === 0 && t('logger.noMatches')}
        {matches.map((match) => (
          <button type="button" key={match.exerciseId} className="exercise-search-result" onClick={() => pick(match)}>
            {match.exerciseName} · {match.muscleGroupName}
          </button>
        ))}
      </div>
    </div>
  );
}
