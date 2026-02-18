export interface AuthResponse {
  readonly accessToken: string;
  readonly expiresAt: string;
  readonly email: string;
  readonly firstName: string;
  readonly lastName: string;
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
}

export interface ApiError {
  readonly status: number;
  readonly title: string;
  readonly detail: string;
  readonly errors?: Record<string, string[]>;
  readonly traceId?: string;
}
