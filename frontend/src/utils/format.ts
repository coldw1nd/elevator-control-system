import type {
  PassengerStatus,
  SessionStatus,
  UserRole,
  ElevatorMovementState,
  Direction
} from '@/types/api';

const dateFormatter = new Intl.DateTimeFormat('ru-RU', {
  dateStyle: 'short',
  timeStyle: 'medium'
});

function getRussianPluralForm(
  value: number,
  one: string,
  few: string,
  many: string
): string {
  const absoluteValue = Math.abs(Math.trunc(value));
  const lastTwoDigits = absoluteValue % 100;
  const lastDigit = absoluteValue % 10;

  if (lastTwoDigits >= 11 && lastTwoDigits <= 14) {
    return many;
  }

  if (lastDigit === 1) {
    return one;
  }

  if (lastDigit >= 2 && lastDigit <= 4) {
    return few;
  }

  return many;
}

export function formatCountWithWord(
  value: number,
  one: string,
  few: string,
  many: string
): string {
  return `${value} ${getRussianPluralForm(value, one, few, many)}`;
}

export function formatFloorCount(value?: number | null): string {
  if (value === null || value === undefined || !Number.isFinite(value)) {
    return '—';
  }

  return formatCountWithWord(Math.trunc(value), 'этаж', 'этажа', 'этажей');
}

export function formatDateTime(value?: string | null): string {
  if (!value) {
    return '—';
  }

  const date = new Date(value);

  if (Number.isNaN(date.getTime())) {
    return '—';
  }

  return dateFormatter.format(date);
}

export function formatWeight(value?: number | null): string {
  if (value === null || value === undefined) {
    return '—';
  }

  return `${value.toFixed(2)} кг`;
}

export function formatRole(role?: UserRole | null): string {
  switch (role) {
    case 'Admin':
      return 'Администратор';
    case 'Operator':
      return 'Оператор';
    case 'Viewer':
      return 'Наблюдатель';
    default:
      return '—';
  }
}

export function formatSessionStatus(status?: SessionStatus | null): string {
  switch (status) {
    case 'Draft':
      return 'Черновик';
    case 'Running':
      return 'Запущен';
    case 'Stopped':
      return 'Остановлен';
    default:
      return '—';
  }
}

export function formatPassengerStatus(status?: PassengerStatus | null): string {
  switch (status) {
    case 'Created':
      return 'Создан';
    case 'WaitingElevator':
      return 'Ожидает лифт';
    case 'Riding':
      return 'В лифте';
    case 'Delivered':
      return 'Доставлен';
    case 'Archived':
      return 'Архивирован';
    default:
      return '—';
  }
}

export function formatMovementState(state?: ElevatorMovementState | null): string {
  switch (state) {
    case 'IdleClosed':
      return 'Стоит, двери закрыты';
    case 'IdleOpen':
      return 'Стоит, двери открыты';
    case 'MovingUp':
      return 'Движется вверх';
    case 'MovingDown':
      return 'Движется вниз';
    default:
      return '—';
  }
}

export function formatDirection(direction?: Direction | null): string {
  switch (direction) {
    case 'Up':
      return 'Вверх';
    case 'Down':
      return 'Вниз';
    case 'None':
      return 'Нет';
    default:
      return '—';
  }
}

export function getSessionStatusColor(status?: SessionStatus | null): string {
  switch (status) {
    case 'Draft':
      return 'warning';
    case 'Running':
      return 'success';
    case 'Stopped':
      return 'secondary';
    default:
      return 'info';
  }
}

export function getPassengerStatusColor(status?: PassengerStatus | null): string {
  switch (status) {
    case 'Created':
      return 'info';
    case 'WaitingElevator':
      return 'warning';
    case 'Riding':
      return 'primary';
    case 'Delivered':
      return 'success';
    case 'Archived':
      return 'secondary';
    default:
      return 'info';
  }
}

export function getMovementStateColor(state?: ElevatorMovementState | null): string {
  switch (state) {
    case 'IdleClosed':
      return 'secondary';
    case 'IdleOpen':
      return 'info';
    case 'MovingUp':
      return 'success';
    case 'MovingDown':
      return 'warning';
    default:
      return 'info';
  }
}

export function toLocalDateTimeInputValue(value?: string | null): string {
  if (!value) {
    return '';
  }

  const date = new Date(value);

  if (Number.isNaN(date.getTime())) {
    return '';
  }

  const offsetMs = date.getTimezoneOffset() * 60_000;
  const localDate = new Date(date.getTime() - offsetMs);

  return localDate.toISOString().slice(0, 16);
}

export function formatRoundedPosition(position?: number | null): string {
  if (position === null || position === undefined) {
    return '—';
  }

  return position.toFixed(1);
}
