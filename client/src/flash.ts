import type { Text } from './i18n/i18n';

/**
 * The state of a navigation that shows a message once on the next page. The message travels as a text key
 * and Layout translates it, so it follows a change of the interface language.
 */
export interface FlashState {
  message?: Text;
}

export function flash(message: Text): FlashState {
  return { message };
}
