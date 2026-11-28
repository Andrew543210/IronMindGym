import type { ChangeEvent } from 'react';

type TextField<T> = { [K in keyof T]: T[K] extends string ? K : never }[keyof T] & string;

// Each text field gets the id its label points at, its value and a handler that replaces only that field.
export function textFields<T extends object>(form: T, change: (next: T) => void) {
  return (name: TextField<T>) => ({
    id: name,
    value: form[name] as string,
    onChange: (event: ChangeEvent<HTMLInputElement | HTMLSelectElement | HTMLTextAreaElement>) =>
      change({ ...form, [name]: event.target.value }),
  });
}
