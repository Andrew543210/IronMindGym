import { describe, expect, it } from 'vitest';
import css from './monochrome.css?raw';

/** The selectors of every rule, without comments and declarations. */
function selectors(stylesheet: string): string[] {
  return stylesheet
    .replace(/\/\*[\s\S]*?\*\//g, '')
    .replace(/\{[^{}]*\}/g, '\n')
    .split(/[\n,]/)
    .map((selector) => selector.replace(/@media[^{]*\{?/, '').trim())
    .filter((selector) => selector !== '' && selector !== '}');
}

describe('stylesheet', () => {
  // Rules on ids outlived the markup they were written for; classes stay with the components that use them.
  it('styles elements by class, not by id', () => {
    expect(selectors(css).filter((selector) => /#[A-Za-z]/.test(selector))).toEqual([]);
  });
});
