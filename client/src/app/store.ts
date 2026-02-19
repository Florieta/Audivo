import { configureStore } from '@reduxjs/toolkit';
import authReducer from '../features/auth/authSlice';
import playerReducer from '../features/player/playerSlice';
import libraryReducer from '../features/library/librarySlice';

export const store = configureStore({
  reducer: {
    auth: authReducer,
    player: playerReducer,
    library: libraryReducer,
  },
});

export type RootState = ReturnType<typeof store.getState>;
export type AppDispatch = typeof store.dispatch;
