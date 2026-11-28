import type { Text } from './i18n/i18n';

// The message travels as a text key, so it follows a change of the interface language.
export interface FlashState {
  message?: Text;
}

export function flash(message: Text): FlashState {
  return { message };
}
