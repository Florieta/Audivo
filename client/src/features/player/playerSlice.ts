import { createAsyncThunk, createSlice } from '@reduxjs/toolkit';
import axios from 'axios';
import { playerService } from '../../services/playerService';
import type { ApiError, BookmarkItem, PlayerState } from '../../types';

interface PlayerSliceState {
  readonly playerState: PlayerState | null;
  readonly isLoading: boolean;
  readonly error: string | null;
  readonly isSavingProgress: boolean;
}

const initialState: PlayerSliceState = {
  playerState: null,
  isLoading: false,
  error: null,
  isSavingProgress: false,
};

const toMessage = (error: unknown): string => {
  if (axios.isAxiosError<ApiError>(error)) {
    return error.response?.data?.detail ?? error.message;
  }
  return error instanceof Error ? error.message : 'Unexpected error';
};

export const fetchPlayerState = createAsyncThunk(
  'player/fetchPlayerState',
  async (audiobookId: string, { rejectWithValue }) => {
    try {
      return await playerService.getPlayerState(audiobookId);
    } catch (error) {
      return rejectWithValue(toMessage(error));
    }
  },
);

export const saveProgressAsync = createAsyncThunk(
  'player/saveProgress',
  async (
    { audiobookId, positionSeconds }: { audiobookId: string; positionSeconds: number },
    { rejectWithValue },
  ) => {
    try {
      await playerService.saveProgress(audiobookId, positionSeconds);
      return positionSeconds;
    } catch (error) {
      return rejectWithValue(toMessage(error));
    }
  },
);

export const createBookmarkAsync = createAsyncThunk(
  'player/createBookmark',
  async (
    { audiobookId, positionSeconds, label }: { audiobookId: string; positionSeconds: number; label?: string },
    { rejectWithValue },
  ) => {
    try {
      return await playerService.createBookmark(audiobookId, positionSeconds, label);
    } catch (error) {
      return rejectWithValue(toMessage(error));
    }
  },
);

export const deleteBookmarkAsync = createAsyncThunk(
  'player/deleteBookmark',
  async (
    { audiobookId, bookmarkId }: { audiobookId: string; bookmarkId: string },
    { rejectWithValue },
  ) => {
    try {
      await playerService.deleteBookmark(audiobookId, bookmarkId);
      return bookmarkId;
    } catch (error) {
      return rejectWithValue(toMessage(error));
    }
  },
);

const playerSlice = createSlice({
  name: 'player',
  initialState,
  reducers: {
    clearPlayerState(state) {
      state.playerState = null;
      state.error = null;
    },
  },
  extraReducers: (builder) => {
    builder
      .addCase(fetchPlayerState.pending, (state) => {
        state.isLoading = true;
        state.error = null;
      })
      .addCase(fetchPlayerState.fulfilled, (state, action) => {
        state.isLoading = false;
        state.playerState = action.payload;
      })
      .addCase(fetchPlayerState.rejected, (state, action) => {
        state.isLoading = false;
        state.error = (action.payload as string) ?? 'Failed to load player';
      })
      .addCase(saveProgressAsync.pending, (state) => {
        state.isSavingProgress = true;
      })
      .addCase(saveProgressAsync.fulfilled, (state, action) => {
        state.isSavingProgress = false;
        if (state.playerState) {
          state.playerState = { ...state.playerState, lastPositionSeconds: action.payload };
        }
      })
      .addCase(saveProgressAsync.rejected, (state) => {
        state.isSavingProgress = false;
      })
      .addCase(createBookmarkAsync.fulfilled, (state, action) => {
        if (state.playerState) {
          const bookmarks = [...state.playerState.bookmarks, action.payload]
            .sort((a, b) => a.positionSeconds - b.positionSeconds);
          state.playerState = { ...state.playerState, bookmarks };
        }
      })
      .addCase(deleteBookmarkAsync.fulfilled, (state, action) => {
        if (state.playerState) {
          state.playerState = {
            ...state.playerState,
            bookmarks: state.playerState.bookmarks.filter(
              (b: BookmarkItem) => b.id !== action.payload,
            ),
          };
        }
      });
  },
});

export const { clearPlayerState } = playerSlice.actions;
export default playerSlice.reducer;
