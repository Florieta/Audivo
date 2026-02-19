import type { ProfileResponse, UpdateProfileRequest } from '../types';
import apiClient from './apiClient';

const PROFILE_BASE = '/api/v1/profile';

export const profileService = {
  async getCurrentProfile(): Promise<ProfileResponse> {
    const { data } = await apiClient.get<ProfileResponse>(`${PROFILE_BASE}/me`);
    return data;
  },

  async updateCurrentProfile(request: UpdateProfileRequest): Promise<ProfileResponse> {
    const formData = new FormData();
    formData.append('firstName', request.firstName);
    formData.append('lastName', request.lastName);
    if (request.profileImage) {
      formData.append('profileImage', request.profileImage);
    }

    const { data } = await apiClient.put<ProfileResponse>(`${PROFILE_BASE}/me`, formData, {
      headers: {
        'Content-Type': 'multipart/form-data',
      },
    });

    return data;
  },
};
