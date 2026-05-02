import type { CurrentUserDto, LoginRequestDto, LoginResponseDto } from '@/types/api';
import { apiClient } from '@/services/http-client';
import {
  clearAuthSession,
  isAuthenticated,
  saveAuthSession,
  saveCurrentUser
} from '@/services/auth-storage';

export const authService = {
  isAuthenticated,

  async login(request: LoginRequestDto): Promise<LoginResponseDto> {
    const { data } = await apiClient.post<LoginResponseDto>('/api/auth/login', request);

    saveAuthSession(data);

    return data;
  },

  async getCurrentUser(): Promise<CurrentUserDto> {
    const { data } = await apiClient.get<CurrentUserDto>('/api/auth/me');

    saveCurrentUser(data);

    return data;
  },

  async logout(): Promise<void> {
    try {
      await apiClient.post('/api/auth/logout');
    } finally {
      clearAuthSession();
    }
  }
};
