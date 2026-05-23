import { describe, expect, it } from 'vitest';
import {
  formatDateTime,
  formatFloorCount,
  formatPassengerStatus,
  formatRole,
  formatSessionStatus,
  formatWeight
} from './format';

describe('format utilities', () => {
  it('formats floor count with correct Russian plural forms', () => {
    expect(formatFloorCount(1)).toBe('1 этаж');
    expect(formatFloorCount(2)).toBe('2 этажа');
    expect(formatFloorCount(5)).toBe('5 этажей');
  });

  it('returns dash for empty floor count', () => {
    expect(formatFloorCount(null)).toBe('—');
    expect(formatFloorCount(undefined)).toBe('—');
  });

  it('returns dash for empty or invalid date', () => {
    expect(formatDateTime(null)).toBe('—');
    expect(formatDateTime(undefined)).toBe('—');
    expect(formatDateTime('invalid-date')).toBe('—');
  });

  it('formats weight with two decimal places', () => {
    expect(formatWeight(70)).toBe('70.00 кг');
    expect(formatWeight(82.5)).toBe('82.50 кг');
  });

  it('formats admin role', () => {
    expect(formatRole('Admin')).toBe('Администратор');
  });

  it('formats running session status', () => {
    expect(formatSessionStatus('Running')).toBe('Запущен');
  });

  it('formats riding passenger status', () => {
    expect(formatPassengerStatus('Riding')).toBe('В лифте');
  });
});
