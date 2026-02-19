import { createAsyncThunk, createSlice } from '@reduxjs/toolkit';
import axios from 'axios';
import type { ApiError, LibraryAudiobook } from '../../types';
import { libraryService } from '../../services/libraryService';

interface LibraryState {
  readonly uploadedBooks: LibraryAudiobook[];
  readonly favoriteBooks: LibraryAudiobook[];
  readonly searchResults: LibraryAudiobook[];
  readonly isLoading: boolean;
  readonly isSearching: boolean;
  readonly error: string | null;
}

const initialState: LibraryState = {
  uploadedBooks: [],
  favoriteBooks: [],
  searchResults: [],
  isLoading: false,
  isSearching: false,
  error: null,
};

const toMessage = (error: unknown) => {
  if (axios.isAxiosError<ApiError>(error)) {
    return error.response?.data?.detail ?? error.message;
  }

  return error instanceof Error ? error.message : 'Unexpected error';
};

export const fetchLibrarySections = createAsyncThunk(
  'library/fetchLibrarySections',
  async (_, { rejectWithValue }) => {
    try {
      const [uploadedBooks, favoriteBooks] = await Promise.all([
        libraryService.getUploadedBooks(),
        libraryService.getFavoriteBooks(),
      ]);

      return { uploadedBooks, favoriteBooks };
    } catch (error) {
      return rejectWithValue(toMessage(error));
    }
  },
);

export const searchLibraryAudiobooks = createAsyncThunk(
  'library/searchLibraryAudiobooks',
  async (searchTerm: string, { rejectWithValue }) => {
    try {
      if (!searchTerm.trim()) {
        return [] as LibraryAudiobook[];
      }

      return await libraryService.searchAudiobooks(searchTerm);
    } catch (error) {
      return rejectWithValue(toMessage(error));
    }
  },
);

export const addFavoriteAsync = createAsyncThunk(
  'library/addFavorite',
  async (audiobookId: string, { dispatch, rejectWithValue }) => {
    try {
      await libraryService.addFavorite(audiobookId);
      await dispatch(fetchLibrarySections());
      return audiobookId;
    } catch (error) {
      return rejectWithValue(toMessage(error));
    }
  },
);

export const removeFavoriteAsync = createAsyncThunk(
  'library/removeFavorite',
  async (audiobookId: string, { dispatch, rejectWithValue }) => {
    try {
      await libraryService.removeFavorite(audiobookId);
      await dispatch(fetchLibrarySections());
      return audiobookId;
    } catch (error) {
      return rejectWithValue(toMessage(error));
    }
  },
);

const librarySlice = createSlice({
  name: 'library',
  initialState,
  reducers: {
    clearLibrarySearch(state) {
      state.searchResults = [];
    },
  },
  extraReducers: (builder) => {
    builder
      .addCase(fetchLibrarySections.pending, (state) => {
        state.isLoading = true;
        state.error = null;
      })
      .addCase(fetchLibrarySections.fulfilled, (state, action) => {
        state.isLoading = false;
        state.uploadedBooks = action.payload.uploadedBooks;
        state.favoriteBooks = action.payload.favoriteBooks;
      })
      .addCase(fetchLibrarySections.rejected, (state, action) => {
        state.isLoading = false;
        state.error = (action.payload as string | undefined) ?? 'Failed to load library';
      })
      .addCase(searchLibraryAudiobooks.pending, (state) => {
        state.isSearching = true;
      })
      .addCase(searchLibraryAudiobooks.fulfilled, (state, action) => {
        state.isSearching = false;
        state.searchResults = action.payload;
      })
      .addCase(searchLibraryAudiobooks.rejected, (state, action) => {
        state.isSearching = false;
        state.error = (action.payload as string | undefined) ?? 'Failed to search audiobooks';
      })
      .addCase(addFavoriteAsync.fulfilled, (state, action) => {
        state.searchResults = state.searchResults.map((item) =>
          item.id === action.payload ? { ...item, isFavorite: true } : item,
        );
      })
      .addCase(addFavoriteAsync.rejected, (state, action) => {
        state.error = (action.payload as string | undefined) ?? 'Failed to add favorite';
      })
      .addCase(removeFavoriteAsync.fulfilled, (state, action) => {
        state.searchResults = state.searchResults.map((item) =>
          item.id === action.payload ? { ...item, isFavorite: false } : item,
        );
      })
      .addCase(removeFavoriteAsync.rejected, (state, action) => {
        state.error = (action.payload as string | undefined) ?? 'Failed to remove favorite';
      });
  },
});

export const { clearLibrarySearch } = librarySlice.actions;
export default librarySlice.reducer;
