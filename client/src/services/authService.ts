import type { AuthResponse, LoginRequest, RegisterRequest } from '../types';
import apiClient from './apiClient';

const AUTH_BASE = '/api/v1/auth';

export const authService = {
  async login(request: LoginRequest): Promise<AuthResponse> {
    const { data } = await apiClient.post<AuthResponse>(`${AUTH_BASE}/login`, request);
    return data;
  },

  async register(request: RegisterRequest): Promise<AuthResponse> {
    const { data } = await apiClient.post<AuthResponse>(`${AUTH_BASE}/register`, request);
    return data;
  },

  async refreshToken(): Promise<AuthResponse> {
    const { data } = await apiClient.post<AuthResponse>(`${AUTH_BASE}/refresh-token`);
    return data;
  },

  async revokeToken(): Promise<void> {
    await apiClient.post(`${AUTH_BASE}/revoke-token`);
  },
};
