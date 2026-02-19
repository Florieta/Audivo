import axios from 'axios';
import { store } from '../app/store';
import { logout } from '../features/auth/authSlice';

const apiClient = axios.create({
  baseURL: import.meta.env.VITE_API_URL || 'https://localhost:7001',
  headers: {
    'Content-Type': 'application/json',
  },
  withCredentials: true, // Required for httpOnly cookie refresh tokens
});

// Request interceptor: attach access token from Redux store
apiClient.interceptors.request.use(
  (config) => {
    const { accessToken } = store.getState().auth;
    if (accessToken) {
      config.headers.Authorization = `Bearer ${accessToken}`;
    }
    return config;
  },
  (error) => Promise.reject(error),
);

// Response interceptor: handle 401 with silent refresh
let isRefreshing = false;
let failedQueue: Array<{
  resolve: (value: unknown) => void;
  reject: (reason: unknown) => void;
}> = [];

const processQueue = (error: unknown) => {
  failedQueue.forEach((prom) => {
    if (error) {
      prom.reject(error);
    } else {
      prom.resolve(undefined);
    }
  });
  failedQueue = [];
};

apiClient.interceptors.response.use(
  (response) => response,
  async (error) => {
    const originalRequest = error.config;
    const requestUrl = (originalRequest?.url as string | undefined)?.toLowerCase() ?? '';
    const isRefreshEndpoint = requestUrl.includes('/api/v1/auth/refresh-token');

    if (error.response?.status === 401 && originalRequest && !originalRequest._retry && !isRefreshEndpoint) {
      if (isRefreshing) {
        return new Promise((resolve, reject) => {
          failedQueue.push({ resolve, reject });
        }).then(() => apiClient(originalRequest));
      }

      originalRequest._retry = true;
      isRefreshing = true;

      try {
        // Attempt silent refresh — the refresh token cookie is sent automatically
        const { data } = await apiClient.post('/api/v1/auth/refresh-token');

        // Dispatch the new token to the store
        const { setCredentials } = await import('../features/auth/authSlice');
        store.dispatch(
          setCredentials({
            accessToken: data.accessToken,
            user: {
              email: data.email,
              firstName: data.firstName,
              lastName: data.lastName,
              profileImageUrl: data.profileImageUrl,
            },
          }),
        );

        processQueue(null);
        return apiClient(originalRequest);
      } catch (refreshError) {
        processQueue(refreshError);
        store.dispatch(logout());
        return Promise.reject(refreshError);
      } finally {
        isRefreshing = false;
      }
    }

    return Promise.reject(error);
  },
);

export default apiClient;
