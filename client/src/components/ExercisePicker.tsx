import type { Ref } from 'react';
import type { CatalogExercise } from '../api/types/exercises';
import { useI18n } from '../i18n/i18n';
import type { ExerciseChoice } from './exerciseChoice';

interface ExercisePickerProps {
  catalog: CatalogExercise[];
  choice: ExerciseChoice;
  onChoose: (next: ExerciseChoice) => void;
  disabled?: boolean;
  /** Folds the group filters under this summary and leaves the exercise list in view. */
  foldFiltersUnder?: string;
  exerciseRef?: Ref<HTMLSelectElement>;
}

/** Narrows the catalog by muscle group and focus area to one exercise; it renders columns of a Bootstrap row. */
export function ExercisePicker({ catalog, choice, onChoose, disabled = false, foldFiltersUnder, exerciseRef }: ExercisePickerProps) {
  const { t, language } = useI18n();
  const groups = [...new Map(catalog.map((e) => [e.muscleGroupId, e.muscleGroupName]))].sort((a, b) => a[1].localeCompare(b[1], language));
  const inGroup = catalog.filter((e) => e.muscleGroupId === Number(choice.groupId));
  const subGroups = [...new Map(inGroup.map((e) => [e.muscleSubGroupId, e.muscleSubGroupName]))];
  const exercises = inGroup.filter((e) => !choice.subGroupId || e.muscleSubGroupId === Number(choice.subGroupId));
  const column = foldFiltersUnder ? 'col-md-6' : 'col-md-4';

  const filters = (
    <>
      <div className={column}>
        <label htmlFor="muscleGroupSelect" className="form-label">{t('picker.group')}</label>
        <select id="muscleGroupSelect" className="form-select" value={choice.groupId} disabled={disabled}
          onChange={(e) => onChoose({ groupId: e.target.value, subGroupId: '', exerciseId: '' })}>
          <option value="">{t('picker.chooseGroup')}</option>
          {groups.map(([id, name]) => <option key={id} value={id}>{name}</option>)}
        </select>
      </div>
      <div className={column}>
        <label htmlFor="muscleSubGroupSelect" className="form-label">{t('picker.focusArea')}</label>
        <select id="muscleSubGroupSelect" className="form-select" value={choice.subGroupId} disabled={subGroups.length === 0 || disabled}
          onChange={(e) => onChoose({ ...choice, subGroupId: e.target.value, exerciseId: '' })}>
          <option value="">{t('picker.allFocusAreas')}</option>
          {subGroups.map(([id, name]) => <option key={id} value={id}>{name}</option>)}
        </select>
      </div>
    </>
  );

  const exercise = (
    <div className={foldFiltersUnder ? 'col-12' : column}>
      <label htmlFor="exerciseSelect" className="form-label">{t('common.exercise')}</label>
      <select id="exerciseSelect" ref={exerciseRef} className="form-select" value={choice.exerciseId} disabled={exercises.length === 0 || disabled}
        onChange={(e) => onChoose({ ...choice, exerciseId: e.target.value })}>
        <option value="">{t('picker.chooseExercise')}</option>
        {exercises.map((e) => <option key={e.exerciseId} value={e.exerciseId}>{e.exerciseName}</option>)}
      </select>
    </div>
  );

  if (!foldFiltersUnder) return <>{filters}{exercise}</>;
  return (
    <>
      <div className="col-12">
        <details>
          <summary>{foldFiltersUnder}</summary>
          <div className="row g-3 mt-1">{filters}</div>
        </details>
      </div>
      {exercise}
    </>
  );
}
