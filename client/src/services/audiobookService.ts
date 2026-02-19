import type { Audiobook, AudiobookFormState } from '../types';
import apiClient from './apiClient';

const AUDIOBOOKS_BASE = '/api/v1/audiobooks';

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

export const audiobookService = {
  async getMyAudiobooks(): Promise<Audiobook[]> {
    const { data } = await apiClient.get<Audiobook[]>(AUDIOBOOKS_BASE);
    return data;
  },

  async createAudiobook(payload: AudiobookFormState): Promise<Audiobook> {
    const formData = toFormData(payload);
    const { data } = await apiClient.post<Audiobook>(AUDIOBOOKS_BASE, formData, {
      headers: { 'Content-Type': 'multipart/form-data' },
    });
    return data;
  },

  async updateAudiobook(id: string, payload: AudiobookFormState): Promise<Audiobook> {
    const formData = toFormData(payload);
    const { data } = await apiClient.put<Audiobook>(`${AUDIOBOOKS_BASE}/${id}`, formData, {
      headers: { 'Content-Type': 'multipart/form-data' },
    });
    return data;
  },

  async deleteAudiobook(id: string): Promise<void> {
    await apiClient.delete(`${AUDIOBOOKS_BASE}/${id}`);
  },
};
