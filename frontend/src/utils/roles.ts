import type { CurrentUserDto, UserRole } from '@/types/api';

export function hasRole(user: CurrentUserDto | null, roles: UserRole[]): boolean {
  if (!user) {
    return false;
  }

  if (roles.length === 0) {
    return true;
  }

  return roles.includes(user.role);
}

export function canManageSimulation(user: CurrentUserDto | null): boolean {
  return hasRole(user, ['Admin', 'Operator']);
}

export function isAdmin(user: CurrentUserDto | null): boolean {
  return hasRole(user, ['Admin']);
}
