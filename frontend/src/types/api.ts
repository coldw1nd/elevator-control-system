export type UserRole = 'Admin' | 'Operator' | 'Viewer';
export type SessionStatus = 'Draft' | 'Running' | 'Stopped';
export type PassengerStatus = 'Created' | 'WaitingElevator' | 'Riding' | 'Delivered' | 'Archived';
export type Direction = 'None' | 'Up' | 'Down';
export type ElevatorMovementState = 'IdleClosed' | 'IdleOpen' | 'MovingUp' | 'MovingDown';
export type SnackbarColor = 'success' | 'error' | 'warning' | 'info';
export type SimulationConnectionStatus = 'disconnected' | 'connecting' | 'connected' | 'reconnecting';

export interface LoginRequestDto {
  username: string;
  password: string;
}

export interface CurrentUserDto {
  id: string;
  username: string;
  displayName: string;
  role: UserRole;
  isActive: boolean;
  lastLoginAtUtc?: string | null;
}

export interface LoginResponseDto {
  accessToken: string;
  expiresAtUtc: string;
  user: CurrentUserDto;
}

export interface CreatePassengerRequestDto {
  weightKg: number;
  sourceFloor: number;
  targetFloor: number;
}

export interface UpdatePassengerRequestDto {
  weightKg: number;
  sourceFloor: number;
  targetFloor: number;
}

export interface CreateSessionRequestDto {
  name?: string | null;
  floorCount: number;
  initialPassengers: CreatePassengerRequestDto[];
}

export interface SessionListItemDto {
  id: string;
  name: string;
  floorCount: number;
  status: SessionStatus;
  createdAtUtc: string;
  startedAtUtc?: string | null;
  stoppedAtUtc?: string | null;
  totalPassengers: number;
  hasReport: boolean;
}

export interface ElevatorStateDto {
  currentFloor: number;
  currentPosition: number;
  movementState: ElevatorMovementState;
  direction: Direction;
  doorsAreOpen: boolean;
  overloadIndicatorOn: boolean;
  currentLoadKg: number;
  maxLoadKg: number;
  passengerCount: number;
  goCommandPending: boolean;
  awaitingGoCommand: boolean;
}

export interface ButtonStateDto {
  floorNumber: number;
  isPressed: boolean;
}

export interface PassengerDto {
  id: string;
  weightKg: number;
  sourceFloor: number;
  targetFloor: number;
  currentFloor: number;
  status: PassengerStatus;
  statusDescription: string;
  locationDescription: string;
  isInElevator: boolean;
  createdAtUtc: string;
  callPressedAtUtc?: string | null;
  boardedAtUtc?: string | null;
  deliveredAtUtc?: string | null;
}

export interface StatusBarDto {
  movingElevatorsCount: number;
  stoppedElevatorsCount: number;
  transportedPassengersCount: number;
}

export interface SessionSnapshotDto {
  sessionId: string;
  sessionName: string;
  status: SessionStatus;
  stopRequested: boolean;
  floorCount: number;
  elevator: ElevatorStateDto;
  floorCalls: ButtonStateDto[];
  cabinRequests: ButtonStateDto[];
  passengers: PassengerDto[];
  statusBar: StatusBarDto;
  serverTimeUtc: string;
}

export interface PassengerLocationDto {
  passengerId: string;
  locationDescription: string;
  status: PassengerStatus;
  currentFloor: number;
}

export interface SessionReportDto {
  sessionId: string;
  sessionName: string;
  totalTrips: number;
  emptyTrips: number;
  totalTransportedWeightKg: number;
  totalCreatedPassengers: number;
  sessionStartedAtUtc?: string | null;
  sessionStoppedAtUtc?: string | null;
  generatedAtUtc: string;
  passengers: PassengerDto[];
}

export interface CreateUserRequestDto {
  username: string;
  displayName: string;
  password: string;
  role: UserRole;
}

export interface UpdateUserRequestDto {
  username: string;
  displayName: string;
  password?: string | null;
  role: UserRole;
  isActive: boolean;
}

export interface UserDto {
  id: string;
  username: string;
  displayName: string;
  role: UserRole;
  isActive: boolean;
  createdAtUtc: string;
  lastLoginAtUtc?: string | null;
}

export interface AuditLogDto {
  id: string;
  userId?: string | null;
  username: string;
  action: string;
  entityType: string;
  entityId?: string | null;
  details: string;
  ipAddress: string;
  createdAtUtc: string;
}

export interface AuditLogQueryDto {
  username?: string;
  action?: string;
  entityType?: string;
  sessionId?: string;
  fromUtc?: string;
  toUtc?: string;
  limit?: number;
}

export interface ApiProblemDetails {
  title?: string;
  detail?: string;
  status?: number;
}

export interface NavItem {
  key: string;
  title: string;
  href: string;
  roles: UserRole[];
  icon: string;
}
