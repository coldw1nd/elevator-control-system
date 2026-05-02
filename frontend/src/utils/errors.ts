import axios from 'axios';

export function extractErrorMessage(error: unknown): string {
  if (axios.isAxiosError(error)) {
    const detail = error.response?.data?.detail;
    const title = error.response?.data?.title;

    if (typeof detail === 'string' && detail.trim().length > 0) {
      return detail;
    }

    if (typeof title === 'string' && title.trim().length > 0) {
      return title;
    }

    if (typeof error.message === 'string' && error.message.trim().length > 0) {
      return error.message;
    }
  }

  if (error instanceof Error) {
    return error.message;
  }

  return 'Произошла непредвиденная ошибка.';
}
