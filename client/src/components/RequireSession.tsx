import { Navigate, Outlet, useLocation } from 'react-router';
import { useSession } from '../session';
import { PageLoading } from './ui';

/** Signed-out visitors go to the login page and come back afterwards; a new account finishes the setup first. */
export function RequireSession() {
  const { loaded, session } = useSession();
  const location = useLocation();
  if (!loaded) return <PageLoading />;
  if (!session) return <Navigate to="/login" replace state={{ from: location.pathname + location.search }} />;
  if (!session.setupComplete && location.pathname !== '/setup') return <Navigate to="/setup" replace />;
  return <Outlet />;
}
