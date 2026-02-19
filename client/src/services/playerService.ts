import type { BookmarkItem, PlayerState } from '../types';
import apiClient from './apiClient';

const PLAYER_BASE = '/api/v1/player';

export const playerService = {
  async getPlayerState(audiobookId: string): Promise<PlayerState> {
    const { data } = await apiClient.get<PlayerState>(`${PLAYER_BASE}/${audiobookId}`);
    return data;
  },

  async saveProgress(audiobookId: string, positionSeconds: number, totalDurationSeconds?: number): Promise<void> {
    await apiClient.put(`${PLAYER_BASE}/${audiobookId}/progress`, {
      positionSeconds,
      totalDurationSeconds: totalDurationSeconds ?? null,
    });
  },

  async getBookmarks(audiobookId: string): Promise<BookmarkItem[]> {
    const { data } = await apiClient.get<BookmarkItem[]>(
      `${PLAYER_BASE}/${audiobookId}/bookmarks`,
    );
    return data;
  },

  async createBookmark(
    audiobookId: string,
    positionSeconds: number,
    label?: string,
  ): Promise<BookmarkItem> {
    const { data } = await apiClient.post<BookmarkItem>(
      `${PLAYER_BASE}/${audiobookId}/bookmarks`,
      { positionSeconds, label: label || null },
    );
    return data;
  },

  async deleteBookmark(audiobookId: string, bookmarkId: string): Promise<void> {
    await apiClient.delete(`${PLAYER_BASE}/${audiobookId}/bookmarks/${bookmarkId}`);
  },
};
