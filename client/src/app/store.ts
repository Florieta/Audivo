import { configureStore } from '@reduxjs/toolkit';
import authReducer from '../features/auth/authSlice';
import audiobooksReducer from '../features/audiobooks/audiobooksSlice';
import playerReducer from '../features/player/playerSlice';

export const store = configureStore({
  reducer: {
    auth: authReducer,
    audiobooks: audiobooksReducer,
    player: playerReducer,
  },
});

export type RootState = ReturnType<typeof store.getState>;
export type AppDispatch = typeof store.dispatch;
