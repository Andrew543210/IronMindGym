import { Fragment, type ReactNode } from 'react';
import { useI18n, type MessageKey, type ParamsOf } from '../i18n/i18n';

const MARK = '\u0000';

/**
 * A translated sentence whose parameters keep their own markup, such as a number in the metric style.
 * The word order comes from the translation; plural texts need `t` with a count instead.
 */
export function Phrase<K extends MessageKey>({ text, parts }: { text: K; parts: { [Name in keyof ParamsOf<K>]: ReactNode } }) {
  const { t } = useI18n();
  const names = Object.keys(parts) as (keyof typeof parts & string)[];
  // Each parameter becomes a marker the sentence is split at, so the markup can take its place.
  const markers = Object.fromEntries(names.map((name) => [name, `${MARK}${name}${MARK}`])) as ParamsOf<K>;
  const pieces = (t as (key: K, params: ParamsOf<K>) => string)(text, markers).split(MARK);
  return <>{pieces.map((piece, i) => <Fragment key={i}>{i % 2 === 1 ? parts[piece as keyof typeof parts] : piece}</Fragment>)}</>;
}
