import { Navigate, useLocation } from 'react-router-dom';
import { useAppSelector } from '../app/hooks';
import type { ReactNode } from 'react';

interface ProtectedRouteProps {
  readonly children: ReactNode;
}

export default function ProtectedRoute({ children }: ProtectedRouteProps) {
  const { isAuthenticated } = useAppSelector((state) => state.auth);
  const location = useLocation();

  if (!isAuthenticated) {
    return <Navigate to="/login" state={{ from: location }} replace />;
  }

  return <>{children}</>;
}
