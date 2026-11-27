import type { ChangeEvent } from 'react';

/** The names of a form's fields that hold text. */
type TextField<T> = { [K in keyof T]: T[K] extends string ? K : never }[keyof T] & string;

/**
 * Binds the text fields of a form kept as one object: each field gets the id its label points at,
 * its value and a handler that replaces only that field.
 */
export function textFields<T extends object>(form: T, change: (next: T) => void) {
  return (name: TextField<T>) => ({
    id: name,
    value: form[name] as string,
    onChange: (event: ChangeEvent<HTMLInputElement | HTMLSelectElement | HTMLTextAreaElement>) =>
      change({ ...form, [name]: event.target.value }),
  });
}
