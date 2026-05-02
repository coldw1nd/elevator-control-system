import axios from 'axios';
import { clearAuthSession, getAccessToken } from '@/services/auth-storage';
import { redirectToLogin } from '@/utils/urls';

const baseURL = import.meta.env.VITE_API_BASE_URL?.trim() || '';

export const apiClient = axios.create({
  baseURL,
  timeout: 15000
});

apiClient.interceptors.request.use((config) => {
  const accessToken = getAccessToken();

  if (accessToken) {
    config.headers.Authorization = `Bearer ${accessToken}`;
  }

  return config;
});

apiClient.interceptors.response.use(
  (response) => response,
  (error) => {
    const statusCode = error?.response?.status;

    if (statusCode === 401 && window.location.pathname !== '/') {
      clearAuthSession();
      redirectToLogin();
    }

    return Promise.reject(error);
  }
);
