import { createSlice, createAsyncThunk, type PayloadAction } from '@reduxjs/toolkit';
import type { User, LoginRequest, RegisterRequest } from '../../types';
import { authService } from '../../services/authService';

interface AuthState {
  readonly user: User | null;
  readonly accessToken: string | null;
  readonly isAuthenticated: boolean;
  readonly isLoading: boolean;
  readonly error: string | null;
}

const initialState: AuthState = {
  user: null,
  accessToken: null,
  isAuthenticated: false,
  isLoading: false,
  error: null,
};

export const loginAsync = createAsyncThunk('auth/login', async (request: LoginRequest) => {
  const response = await authService.login(request);
  return response;
});

export const registerAsync = createAsyncThunk(
  'auth/register',
  async (request: RegisterRequest) => {
    const response = await authService.register(request);
    return response;
  },
);

export const refreshTokenAsync = createAsyncThunk('auth/refreshToken', async () => {
  const response = await authService.refreshToken();
  return response;
});

export const logoutAsync = createAsyncThunk('auth/logout', async () => {
  await authService.revokeToken();
});

const authSlice = createSlice({
  name: 'auth',
  initialState,
  reducers: {
    setCredentials(
      state,
      action: PayloadAction<{ accessToken: string; user: User }>,
    ) {
      state.accessToken = action.payload.accessToken;
      state.user = action.payload.user;
      state.isAuthenticated = true;
      state.error = null;
    },
    logout(state) {
      state.user = null;
      state.accessToken = null;
      state.isAuthenticated = false;
      state.error = null;
    },
    clearError(state) {
      state.error = null;
    },
  },
  extraReducers: (builder) => {
    // Login
    builder
      .addCase(loginAsync.pending, (state) => {
        state.isLoading = true;
        state.error = null;
      })
      .addCase(loginAsync.fulfilled, (state, action) => {
        state.isLoading = false;
        state.isAuthenticated = true;
        state.accessToken = action.payload.accessToken;
        state.user = {
          email: action.payload.email,
          firstName: action.payload.firstName,
          lastName: action.payload.lastName,
          profileImageUrl: action.payload.profileImageUrl,
        };
      })
      .addCase(loginAsync.rejected, (state, action) => {
        state.isLoading = false;
        state.error = action.error.message ?? 'Login failed';
      });

    // Register
    builder
      .addCase(registerAsync.pending, (state) => {
        state.isLoading = true;
        state.error = null;
      })
      .addCase(registerAsync.fulfilled, (state, action) => {
        state.isLoading = false;
        state.isAuthenticated = true;
        state.accessToken = action.payload.accessToken;
        state.user = {
          email: action.payload.email,
          firstName: action.payload.firstName,
          lastName: action.payload.lastName,
          profileImageUrl: action.payload.profileImageUrl,
        };
      })
      .addCase(registerAsync.rejected, (state, action) => {
        state.isLoading = false;
        state.error = action.error.message ?? 'Registration failed';
      });

    // Refresh
    builder
      .addCase(refreshTokenAsync.fulfilled, (state, action) => {
        state.accessToken = action.payload.accessToken;
        state.isAuthenticated = true;
        state.user = {
          email: action.payload.email,
          firstName: action.payload.firstName,
          lastName: action.payload.lastName,
          profileImageUrl: action.payload.profileImageUrl,
        };
      })
      .addCase(refreshTokenAsync.rejected, (state) => {
        state.user = null;
        state.accessToken = null;
        state.isAuthenticated = false;
      });

    // Logout
    builder.addCase(logoutAsync.fulfilled, (state) => {
      state.user = null;
      state.accessToken = null;
      state.isAuthenticated = false;
    });
  },
});

export const { setCredentials, logout, clearError } = authSlice.actions;
export default authSlice.reducer;
