import type { CurrentUserDto, LoginResponseDto } from '@/types/api';

const accessTokenKey = 'ecs.accessToken';
const accessTokenExpiresKey = 'ecs.accessTokenExpiresAt';
const currentUserKey = 'ecs.currentUser';

export function getAccessToken(): string | null {
  try {
    return sessionStorage.getItem(accessTokenKey);
  } catch {
    return null;
  }
}

export function getAccessTokenExpiresAt(): string | null {
  try {
    return sessionStorage.getItem(accessTokenExpiresKey);
  } catch {
    return null;
  }
}

export function getStoredUser(): CurrentUserDto | null {
  try {
    const rawValue = sessionStorage.getItem(currentUserKey);

    if (!rawValue) {
      return null;
    }

    return JSON.parse(rawValue) as CurrentUserDto;
  } catch {
    return null;
  }
}

export function saveCurrentUser(user: CurrentUserDto): void {
  try {
    sessionStorage.setItem(currentUserKey, JSON.stringify(user));
  } catch {
    // ignore
  }
}

export function saveAuthSession(loginResponse: LoginResponseDto): void {
  try {
    sessionStorage.setItem(accessTokenKey, loginResponse.accessToken);
    sessionStorage.setItem(accessTokenExpiresKey, loginResponse.expiresAtUtc);
    sessionStorage.setItem(currentUserKey, JSON.stringify(loginResponse.user));
  } catch {
    // ignore
  }
}

export function clearAuthSession(): void {
  try {
    sessionStorage.removeItem(accessTokenKey);
    sessionStorage.removeItem(accessTokenExpiresKey);
    sessionStorage.removeItem(currentUserKey);
  } catch {
    // ignore
  }
}

function isTokenExpired(expiresAtUtc: string | null): boolean {
  if (!expiresAtUtc) {
    return true;
  }

  const expiresAt = new Date(expiresAtUtc);

  if (Number.isNaN(expiresAt.getTime())) {
    return true;
  }

  return expiresAt.getTime() <= Date.now();
}

export function isAuthenticated(): boolean {
  const accessToken = getAccessToken();
  const expiresAtUtc = getAccessTokenExpiresAt();

  // Если токен просрочен или поврежден, сразу очищаем локальную сессию.
  // Это позволяет не хранить "мертвые" данные между переходами по MPA-страницам.
  if (!accessToken || isTokenExpired(expiresAtUtc)) {
    clearAuthSession();
    return false;
  }

  return true;
}
