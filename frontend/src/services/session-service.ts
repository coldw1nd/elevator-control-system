import type {
  CreatePassengerRequestDto,
  CreateSessionRequestDto,
  PassengerDto,
  PassengerLocationDto,
  SessionListItemDto,
  SessionSnapshotDto,
  UpdatePassengerRequestDto
} from '@/types/api';
import { apiClient } from '@/services/http-client';

export const sessionService = {
  async getAllSessions(): Promise<SessionListItemDto[]> {
    const { data } = await apiClient.get<SessionListItemDto[]>('/api/sessions');

    return data;
  },

  async getCurrentSession(): Promise<SessionSnapshotDto> {
    const { data } = await apiClient.get<SessionSnapshotDto>('/api/sessions/current');

    return data;
  },

  async getSessionSnapshot(sessionId: string): Promise<SessionSnapshotDto> {
    const { data } = await apiClient.get<SessionSnapshotDto>(`/api/sessions/${sessionId}/snapshot`);

    return data;
  },

  async createSession(request: CreateSessionRequestDto): Promise<SessionSnapshotDto> {
    const { data } = await apiClient.post<SessionSnapshotDto>('/api/sessions', request);

    return data;
  },

  async startSession(sessionId: string): Promise<SessionSnapshotDto> {
    const { data } = await apiClient.post<SessionSnapshotDto>(`/api/sessions/${sessionId}/start`);

    return data;
  },

  async issueGoCommand(sessionId: string): Promise<SessionSnapshotDto> {
    const { data } = await apiClient.post<SessionSnapshotDto>(`/api/sessions/${sessionId}/go`);

    return data;
  },

  async stopSession(sessionId: string): Promise<SessionSnapshotDto> {
    const { data } = await apiClient.post<SessionSnapshotDto>(`/api/sessions/${sessionId}/stop`);

    return data;
  },

  async deleteSession(sessionId: string): Promise<void> {
    await apiClient.delete(`/api/sessions/${sessionId}`);
  },

  async getPassengers(sessionId: string): Promise<PassengerDto[]> {
    const { data } = await apiClient.get<PassengerDto[]>(`/api/sessions/${sessionId}/passengers`);

    return data;
  },

  async createPassenger(
    sessionId: string,
    request: CreatePassengerRequestDto
  ): Promise<SessionSnapshotDto> {
    const { data } = await apiClient.post<SessionSnapshotDto>(
      `/api/sessions/${sessionId}/passengers`,
      request
    );

    return data;
  },

  async updatePassenger(
    sessionId: string,
    passengerId: string,
    request: UpdatePassengerRequestDto
  ): Promise<SessionSnapshotDto> {
    const { data } = await apiClient.put<SessionSnapshotDto>(
      `/api/sessions/${sessionId}/passengers/${passengerId}`,
      request
    );

    return data;
  },

  async deletePassenger(sessionId: string, passengerId: string): Promise<SessionSnapshotDto> {
    const { data } = await apiClient.delete<SessionSnapshotDto>(
      `/api/sessions/${sessionId}/passengers/${passengerId}`
    );

    return data;
  },

  async getPassengerLocation(
    sessionId: string,
    passengerId: string
  ): Promise<PassengerLocationDto> {
    const { data } = await apiClient.get<PassengerLocationDto>(
      `/api/sessions/${sessionId}/passengers/${passengerId}/location`
    );

    return data;
  }
};
