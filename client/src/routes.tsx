import type { RouteObject } from 'react-router';
import { Layout } from './components/Layout';
import { RequireSession } from './components/RequireSession';
import { DashboardPage, FeedNotePage } from './pages/DashboardPage';
import { NotFoundPage } from './pages/NotFoundPage';
import { SetupPage } from './pages/SetupPage';
import { LoginPage, RegisterPage } from './pages/auth/AuthPages';

export const routes: RouteObject[] = [
  {
    element: <Layout />,
    children: [
      { path: '/login', element: <LoginPage /> },
      { path: '/register', element: <RegisterPage /> },
      {
        element: <RequireSession />,
        children: [
          { path: '/', element: <DashboardPage /> },
          { path: '/setup', element: <SetupPage /> },
          { path: '/feed/:id', element: <FeedNotePage /> },
        ],
      },
      { path: '*', element: <NotFoundPage /> },
    ],
  },
];
