import { createAsyncThunk, createSlice } from '@reduxjs/toolkit';
import axios from 'axios';
import { audiobookService } from '../../services/audiobookService';
import type { ApiError, Audiobook, AudiobookFormState } from '../../types';

interface AudiobooksState {
  readonly items: Audiobook[];
  readonly isLoading: boolean;
  readonly error: string | null;
  readonly validationErrors: Record<string, string[]> | null;
}

const initialState: AudiobooksState = {
  items: [],
  isLoading: false,
  error: null,
  validationErrors: null,
};

const toRejection = (error: unknown): { message: string; validationErrors?: Record<string, string[]> } => {
  if (axios.isAxiosError<ApiError>(error)) {
    return {
      message: error.response?.data?.detail ?? error.message,
      validationErrors: error.response?.data?.errors,
    };
  }

  return {
    message: error instanceof Error ? error.message : 'Unexpected error',
  };
};

export const fetchMyAudiobooks = createAsyncThunk(
  'audiobooks/fetchMyAudiobooks',
  async (_, { rejectWithValue }) => {
    try {
      return await audiobookService.getMyAudiobooks();
    } catch (error) {
      return rejectWithValue(toRejection(error));
    }
  },
);

export const createAudiobookAsync = createAsyncThunk(
  'audiobooks/createAudiobook',
  async (payload: AudiobookFormState, { rejectWithValue }) => {
    try {
      return await audiobookService.createAudiobook(payload);
    } catch (error) {
      return rejectWithValue(toRejection(error));
    }
  },
);

export const updateAudiobookAsync = createAsyncThunk(
  'audiobooks/updateAudiobook',
  async ({ id, payload }: { id: string; payload: AudiobookFormState }, { rejectWithValue }) => {
    try {
      return await audiobookService.updateAudiobook(id, payload);
    } catch (error) {
      return rejectWithValue(toRejection(error));
    }
  },
);

export const deleteAudiobookAsync = createAsyncThunk(
  'audiobooks/deleteAudiobook',
  async (id: string, { rejectWithValue }) => {
    try {
      await audiobookService.deleteAudiobook(id);
      return id;
    } catch (error) {
      return rejectWithValue(toRejection(error));
    }
  },
);

const audiobooksSlice = createSlice({
  name: 'audiobooks',
  initialState,
  reducers: {
    clearAudiobookErrors(state) {
      state.error = null;
      state.validationErrors = null;
    },
  },
  extraReducers: (builder) => {
    builder
      .addCase(fetchMyAudiobooks.pending, (state) => {
        state.isLoading = true;
        state.error = null;
        state.validationErrors = null;
      })
      .addCase(fetchMyAudiobooks.fulfilled, (state, action) => {
        state.isLoading = false;
        state.items = action.payload;
      })
      .addCase(fetchMyAudiobooks.rejected, (state, action) => {
        state.isLoading = false;
        state.error = (action.payload as { message?: string } | undefined)?.message ?? 'Failed to load audiobooks';
      })
      .addCase(createAudiobookAsync.pending, (state) => {
        state.error = null;
        state.validationErrors = null;
      })
      .addCase(createAudiobookAsync.fulfilled, (state, action) => {
        state.items = [action.payload, ...state.items];
      })
      .addCase(createAudiobookAsync.rejected, (state, action) => {
        state.error = (action.payload as { message?: string } | undefined)?.message ?? 'Failed to create audiobook';
        state.validationErrors =
          (action.payload as { validationErrors?: Record<string, string[]> } | undefined)?.validationErrors ?? null;
      })
      .addCase(updateAudiobookAsync.pending, (state) => {
        state.error = null;
        state.validationErrors = null;
      })
      .addCase(updateAudiobookAsync.fulfilled, (state, action) => {
        state.items = state.items.map((item) => (item.id === action.payload.id ? action.payload : item));
      })
      .addCase(updateAudiobookAsync.rejected, (state, action) => {
        state.error = (action.payload as { message?: string } | undefined)?.message ?? 'Failed to update audiobook';
        state.validationErrors =
          (action.payload as { validationErrors?: Record<string, string[]> } | undefined)?.validationErrors ?? null;
      })
      .addCase(deleteAudiobookAsync.rejected, (state, action) => {
        state.error = (action.payload as { message?: string } | undefined)?.message ?? 'Failed to delete audiobook';
      })
      .addCase(deleteAudiobookAsync.fulfilled, (state, action) => {
        state.items = state.items.filter((item) => item.id !== action.payload);
      });
  },
});

export const { clearAudiobookErrors } = audiobooksSlice.actions;
export default audiobooksSlice.reducer;
