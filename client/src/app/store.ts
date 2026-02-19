import { configureStore } from '@reduxjs/toolkit';
import authReducer from '../features/auth/authSlice';
import audiobooksReducer from '../features/audiobooks/audiobooksSlice';

export const store = configureStore({
  reducer: {
    auth: authReducer,
    audiobooks: audiobooksReducer,
  },
});

export type RootState = ReturnType<typeof store.getState>;
export type AppDispatch = typeof store.dispatch;
