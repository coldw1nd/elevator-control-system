import type {
  CreateUserRequestDto,
  UpdateUserRequestDto,
  UserDto
} from '@/types/api';
import { apiClient } from '@/services/http-client';

export const adminService = {
  async getUsers(): Promise<UserDto[]> {
    const { data } = await apiClient.get<UserDto[]>('/api/admin/users');

    return data;
  },

  async createUser(request: CreateUserRequestDto): Promise<UserDto> {
    const { data } = await apiClient.post<UserDto>('/api/admin/users', request);

    return data;
  },

  async updateUser(userId: string, request: UpdateUserRequestDto): Promise<UserDto> {
    const { data } = await apiClient.put<UserDto>(`/api/admin/users/${userId}`, request);

    return data;
  },

  async toggleUserActivity(userId: string): Promise<UserDto> {
    const { data } = await apiClient.patch<UserDto>(`/api/admin/users/${userId}/toggle-activity`);

    return data;
  }
};
