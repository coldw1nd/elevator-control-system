import type { CurrentUserDto } from '@/types/api';

export function getQueryParam(name: string): string | null {
  return new URLSearchParams(window.location.search).get(name);
}

export function updateQueryParam(name: string, value: string | null): void {
  const url = new URL(window.location.href);

  if (value) {
    url.searchParams.set(name, value);
  } else {
    url.searchParams.delete(name);
  }

  window.history.replaceState({}, '', `${url.pathname}${url.search}${url.hash}`);
}

export function redirectToLogin(): void {
  const currentPath = `${window.location.pathname}${window.location.search}`;
  const url = new URL(window.location.origin);

  url.pathname = '/';
  url.searchParams.set('returnUrl', currentPath);

  window.location.href = url.toString();
}

function isSafeInternalReturnUrl(returnUrl: string): boolean {
  // Возвращаться после логина разрешаем только внутрь текущего приложения.
  // Это защищает от open redirect, когда в query пытаются подложить внешний адрес.
  if (!returnUrl.startsWith('/')) {
    return false;
  }

  if (returnUrl.startsWith('//')) {
    return false;
  }

  if (returnUrl.includes('://')) {
    return false;
  }

  return true;
}

export function resolvePostLoginUrl(user: CurrentUserDto): string {
  const returnUrl = getQueryParam('returnUrl');

  if (returnUrl && isSafeInternalReturnUrl(returnUrl)) {
    return returnUrl;
  }

  if (user.role === 'Admin') {
    return '/sessions/';
  }

  return '/dashboard/';
}

export function buildDashboardUrl(sessionId?: string | null): string {
  if (!sessionId) {
    return '/dashboard/';
  }

  return `/dashboard/?sessionId=${encodeURIComponent(sessionId)}`;
}

export function buildReportUrl(sessionId?: string | null): string {
  if (!sessionId) {
    return '/reports/';
  }

  return `/reports/?sessionId=${encodeURIComponent(sessionId)}`;
}
