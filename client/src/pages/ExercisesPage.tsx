import { useDeferredValue, useMemo, useState } from 'react';
import { PageState, ResponsiveTable } from '../components/ui';
import { useApi, usePageTitle } from '../hooks';
import { useI18n, type MessageKey } from '../i18n/i18n';
import type { CatalogExercise, ExerciseType } from './workouts/draft';


interface Library {
  muscleGroups: { id: number; name: string }[];
  exercises: CatalogExercise[];
}

const typeLabels: Record<ExerciseType, MessageKey> = {
  StrengthTraining: 'exerciseType.StrengthTraining',
  Cardio: 'exerciseType.Cardio',
  Timed: 'exerciseType.Timed',
};

export function ExercisesPage() {
  const { t, language } = useI18n();
  usePageTitle(t('exercises.title'));
  const { data, error, loading } = useApi<Library>('/api/exercises');
  const [groupId, setGroupId] = useState('');
  const [subGroup, setSubGroup] = useState('');
  const [search, setSearch] = useState('');
  const query = useDeferredValue(search.trim().toLocaleLowerCase(language));

  const groupNames = useMemo(() => new Map(data?.muscleGroups.map((g) => [g.id, g.name])), [data]);
  const inGroup = useMemo(
    () => data?.exercises.filter((e) => groupId === '' || e.muscleGroupId === Number(groupId)) ?? [],
    [data, groupId],
  );
  const subGroups = useMemo(
    () => [...new Set(inGroup.map((e) => e.muscleSubGroupName))].sort((a, b) => a.localeCompare(b, language)),
    [inGroup, language],
  );
  const shown = inGroup.filter((e) => (subGroup === '' || e.muscleSubGroupName === subGroup)
    && (query === '' || `${e.exerciseName}\n${e.description}`.toLocaleLowerCase(language).includes(query)));

  if (!data) return <PageState error={error} loading={loading} />;

  return (
    <>
      <h2>{t('exercises.title')}</h2>
      <p className="text-muted">{t('exercises.intro')}</p>
      <div className="row g-2 mb-4" role="search">
        <div className="col-md-3">
          <label htmlFor="group" className="visually-hidden">{t('exercises.group')}</label>
          <select id="group" className="form-select" value={groupId} onChange={(e) => { setGroupId(e.target.value); setSubGroup(''); }}>
            <option value="">{t('exercises.allGroups')}</option>
            {data.muscleGroups.map((group) => <option key={group.id} value={group.id}>{group.name}</option>)}
          </select>
        </div>
        <div className="col-md-3">
          <label htmlFor="subGroup" className="visually-hidden">{t('exercises.subgroup')}</label>
          <select id="subGroup" className="form-select" value={subGroup} onChange={(e) => setSubGroup(e.target.value)}>
            <option value="">{t('exercises.allSubgroups')}</option>
            {subGroups.map((name) => <option key={name} value={name}>{name}</option>)}
          </select>
        </div>
        <div className="col-md-6">
          <label htmlFor="search" className="visually-hidden">{t('common.search')}</label>
          <input id="search" type="search" className="form-control" placeholder={t('exercises.search')} maxLength={80}
            value={search} onChange={(e) => setSearch(e.target.value)} />
        </div>
      </div>
      <p className="mb-2 text-muted" role="status">{t('exercises.found', { count: shown.length })}</p>
      {shown.length === 0 ? <p className="text-muted">{t('exercises.none')}</p> : (
        <ResponsiveTable className="table table-striped">
          <thead>
            <tr>
              <th>{t('common.exercise')}</th><th>{t('exercises.group')}</th><th>{t('exercises.subgroup')}</th>
              <th>{t('exercises.type')}</th><th>{t('exercises.description')}</th>
            </tr>
          </thead>
          <tbody>
            {shown.map((exercise) => (
              <tr key={exercise.exerciseId}>
                <td>{exercise.exerciseName}</td>
                <td>{groupNames.get(exercise.muscleGroupId)}</td>
                <td>{exercise.muscleSubGroupName}</td>
                <td>{t(typeLabels[exercise.exerciseType])}</td>
                <td className="small">{exercise.description}</td>
              </tr>
            ))}
          </tbody>
        </ResponsiveTable>
      )}
    </>
  );
}
