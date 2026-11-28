import { describe, expect, it } from 'vitest';
import index from './index.css?raw';

const sheets = import.meta.glob<string>(['./*.css', '!./index.css'], { query: '?raw', import: 'default', eager: true });

function selectors(stylesheet: string): string[] {
  return stylesheet
    .replace(/\/\*[\s\S]*?\*\//g, '')
    .replace(/\{[^{}]*\}/g, '\n')
    .split(/[\n,]/)
    .map((selector) => selector.replace(/@media[^{]*\{?/, '').trim())
    .filter((selector) => selector !== '' && selector !== '}');
}

describe('stylesheets', () => {
  it('style elements by class, not by id', () => {
    const byId = Object.entries(sheets).flatMap(([file, css]) => selectors(css).filter((s) => /#[A-Za-z]/.test(s)).map((s) => `${file}: ${s}`));
    expect(byId).toEqual([]);
  });

  it('are all loaded by the index', () => {
    const imported = [...index.matchAll(/@import "(\.\/[\w-]+\.css)";/g)].map((match) => match[1]);
    expect(imported.sort()).toEqual(Object.keys(sheets).sort());
  });

  // The background shorthand also clears background-image, which draws the arrow of every select.
  it('colour form fields without clearing the select arrow', () => {
    const shorthand = Object.entries(sheets).flatMap(([file, css]) =>
      [...css.replace(/\/\*[\s\S]*?\*\//g, '').matchAll(/([^{}]+)\{([^{}]*)\}/g)]
        .filter(([, selector, body]) => /\.form-(control|select)/.test(selector ?? '') && /(^|[;\s])background\s*:/.test(body ?? ''))
        .map(([, selector]) => `${file}: ${(selector ?? '').trim()}`));
    expect(shorthand).toEqual([]);
  });

  it('leave !important to the reduced motion rule', () => {
    const important = Object.entries(sheets).flatMap(([file, css]) => (css.match(/^.*!important.*$/gm) ?? []).map((line) => `${file}: ${line.trim()}`));
    expect(important).toEqual(['./responsive.css: transition: none !important;', './responsive.css: scroll-behavior: auto !important;']);
  });
});
