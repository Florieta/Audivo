import type { Audiobook, AudiobookFormState } from '../types';
import apiClient from './apiClient';

const AUDIOBOOKS_BASE = '/api/v1/audiobooks';

export type UploadProgressCallback = (percent: number) => void;

const appendIfValue = (formData: FormData, key: string, value: string | File | null) => {
  if (value === null || value === '') {
    return;
  }

  formData.append(key, value);
};

const toFormData = (data: AudiobookFormState) => {
  const formData = new FormData();
  appendIfValue(formData, 'title', data.title.trim());
  appendIfValue(formData, 'author', data.author.trim());
  appendIfValue(formData, 'genre', data.genre.trim());
  appendIfValue(formData, 'coverImage', data.coverImage);
  appendIfValue(formData, 'audioFile', data.audioFile);
  return formData;
};

const uploadConfig = (onProgress?: UploadProgressCallback) => ({
  headers: { 'Content-Type': 'multipart/form-data' },
  timeout: 600_000, // 10 min to accommodate large audio files over slow connections
  ...(onProgress && {
    onUploadProgress: (event: { loaded?: number; total?: number }) => {
      if (event.total && event.total > 0) {
        onProgress(Math.round((event.loaded ?? 0) / event.total * 100));
      }
    },
  }),
});

export const audiobookService = {
  async getMyAudiobooks(): Promise<Audiobook[]> {
    const { data } = await apiClient.get<Audiobook[]>(AUDIOBOOKS_BASE);
    return data;
  },

  async createAudiobook(payload: AudiobookFormState, onProgress?: UploadProgressCallback): Promise<Audiobook> {
    const formData = toFormData(payload);
    const { data } = await apiClient.post<Audiobook>(AUDIOBOOKS_BASE, formData, uploadConfig(onProgress));
    return data;
  },

  async updateAudiobook(id: string, payload: AudiobookFormState, onProgress?: UploadProgressCallback): Promise<Audiobook> {
    const formData = toFormData(payload);
    const { data } = await apiClient.put<Audiobook>(`${AUDIOBOOKS_BASE}/${id}`, formData, uploadConfig(onProgress));
    return data;
  },

  async deleteAudiobook(id: string): Promise<void> {
    await apiClient.delete(`${AUDIOBOOKS_BASE}/${id}`);
  },
};
