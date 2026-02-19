import type { LibraryAudiobook } from '../types';
import apiClient from './apiClient';

const BASE_URL = '/api/v1/library';

export const libraryService = {
  async getGalleryBooks(): Promise<LibraryAudiobook[]> {
    const { data } = await apiClient.get<LibraryAudiobook[]>(`${BASE_URL}/gallery`);
    return data;
  },

  async getUploadedBooks(): Promise<LibraryAudiobook[]> {
    const { data } = await apiClient.get<LibraryAudiobook[]>(`${BASE_URL}/uploaded`);
    return data;
  },

  async getFavoriteBooks(): Promise<LibraryAudiobook[]> {
    const { data } = await apiClient.get<LibraryAudiobook[]>(`${BASE_URL}/favorites`);
    return data;
  },

  async searchAudiobooks(searchTerm: string): Promise<LibraryAudiobook[]> {
    const { data } = await apiClient.get<LibraryAudiobook[]>(`${BASE_URL}/search`, {
      params: { query: searchTerm },
    });
    return data;
  },

  async addFavorite(audiobookId: string): Promise<void> {
    await apiClient.post(`${BASE_URL}/favorites/${audiobookId}`);
  },

  async removeFavorite(audiobookId: string): Promise<void> {
    await apiClient.delete(`${BASE_URL}/favorites/${audiobookId}`);
  },
};
