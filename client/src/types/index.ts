export interface AuthResponse {
  readonly accessToken: string;
  readonly expiresAt: string;
  readonly email: string;
  readonly firstName: string;
  readonly lastName: string;
  readonly profileImageUrl: string | null;
}

export interface LoginRequest {
  readonly email: string;
  readonly password: string;
}

export interface RegisterRequest {
  readonly email: string;
  readonly password: string;
  readonly firstName: string;
  readonly lastName: string;
}

export interface User {
  readonly email: string;
  readonly firstName: string;
  readonly lastName: string;
  readonly profileImageUrl: string | null;
}

export interface ProfileResponse {
  readonly firstName: string;
  readonly lastName: string;
  readonly fullName: string;
  readonly email: string;
  readonly profileImageUrl: string | null;
  readonly createdAt: string;
}

export interface UpdateProfileRequest {
  readonly firstName: string;
  readonly lastName: string;
  readonly profileImage: File | null;
}

export interface ApiError {
  readonly status: number;
  readonly title: string;
  readonly detail: string;
  readonly errors?: Record<string, string[]>;
  readonly traceId?: string;
}

export interface Audiobook {
  readonly id: string;
  readonly title: string;
  readonly author: string;
  readonly genre: string | null;
  readonly coverImageUrl: string | null;
  readonly audioFileUrl: string | null;
  readonly totalDurationSeconds: number;
  readonly createdAt: string;
}

export interface AudiobookFormState {
  readonly title: string;
  readonly author: string;
  readonly genre: string;
  readonly coverImage: File | null;
  readonly audioFile: File | null;
}

export interface PlayerState {
  readonly audiobookId: string;
  readonly title: string;
  readonly author: string;
  readonly genre: string | null;
  readonly coverImageUrl: string | null;
  readonly audioFileUrl: string | null;
  readonly totalDurationSeconds: number;
  readonly lastPositionSeconds: number;
  readonly lastListenedAt: string | null;
  readonly bookmarks: BookmarkItem[];
}

export interface BookmarkItem {
  readonly id: string;
  readonly positionSeconds: number;
  readonly label: string | null;
  readonly createdAt: string;
}

export interface LibraryAudiobook {
  readonly id: string;
  readonly title: string;
  readonly author: string;
  readonly genre: string | null;
  readonly description: string | null;
  readonly coverImageUrl: string | null;
  readonly audioFileUrl: string | null;
  readonly totalDurationSeconds: number;
  readonly listenedSeconds: number;
  readonly progressPercent: number;
  readonly isCompleted: boolean;
  readonly lastListenedAt: string | null;
  readonly createdAt: string;
  readonly isFavorite: boolean;
}
