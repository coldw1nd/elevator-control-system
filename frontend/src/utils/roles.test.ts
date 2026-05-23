import { describe, expect, it } from 'vitest';
import type { CurrentUserDto, UserRole } from '@/types/api';
import { canManageSimulation } from './roles';

function createUser(role: UserRole): CurrentUserDto {
  return {
    id: crypto.randomUUID(),
    username: role.toLowerCase(),
    displayName: role,
    role,
    isActive: true,
    lastLoginAtUtc: null
  };
}

describe('role utilities', () => {
  it('allows admin to manage simulation', () => {
    expect(canManageSimulation(createUser('Admin'))).toBe(true);
  });

  it('allows operator to manage simulation', () => {
    expect(canManageSimulation(createUser('Operator'))).toBe(true);
  });

  it('does not allow viewer to manage simulation', () => {
    expect(canManageSimulation(createUser('Viewer'))).toBe(false);
  });
});
