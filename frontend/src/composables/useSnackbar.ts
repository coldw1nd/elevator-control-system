import { reactive } from 'vue';
import type { SnackbarColor } from '@/types/api';

export function useSnackbar() {
  const snackbar = reactive({
    visible: false,
    text: '',
    color: 'info' as SnackbarColor,
    timeout: 4000
  });

  function show(text: string, color: SnackbarColor): void {
    snackbar.text = text;
    snackbar.color = color;
    snackbar.visible = true;
  }

  function showSuccess(text: string): void {
    show(text, 'success');
  }

  function showError(text: string): void {
    show(text, 'error');
  }

  function showWarning(text: string): void {
    show(text, 'warning');
  }

  function showInfo(text: string): void {
    show(text, 'info');
  }

  return {
    snackbar,
    showSuccess,
    showError,
    showWarning,
    showInfo
  };
}
