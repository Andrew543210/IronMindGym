import type { ComponentType } from 'react';
import type { RouteObject } from 'react-router';
import { Layout } from './components/Layout';
import { RequireSession } from './components/RequireSession';
import { RouteError } from './components/RouteError';
import { PageLoading } from './components/ui';
import { LoginPage, RegisterPage } from './pages/auth/AuthPages';
import { NotFoundPage } from './pages/NotFoundPage';

/** Each section is its own bundle, loaded the first time the athlete opens it. */
function page<M>(load: () => Promise<M>, name: keyof M): RouteObject['lazy'] {
  return async () => ({ Component: (await load())[name] as ComponentType });
}

const dashboard = () => import('./pages/DashboardPage');
const statistics = () => import('./pages/StatisticsPages');
const workouts = () => import('./pages/workouts/WorkoutPages');

export const routes: RouteObject[] = [
  {
    element: <Layout />,
    hydrateFallbackElement: <PageLoading />,
    // A page that fails, such as a section bundle a release removed, is replaced inside the layout,
    // so the navigation stays; a failure of the layout itself falls back to the outer handler.
    errorElement: <RouteError />,
    children: [
      {
        errorElement: <RouteError />,
        children: [
          { path: '/login', element: <LoginPage /> },
          { path: '/register', element: <RegisterPage /> },
          {
            element: <RequireSession />,
            children: [
              { path: '/', lazy: page(dashboard, 'DashboardPage') },
              { path: '/setup', lazy: page(() => import('./pages/SetupPage'), 'SetupPage') },
              { path: '/feed/:id', lazy: page(dashboard, 'FeedNotePage') },
              { path: '/goals', lazy: page(() => import('./pages/GoalsPage'), 'GoalsPage') },
              { path: '/workouts', lazy: page(workouts, 'WorkoutHistoryPage') },
              { path: '/workouts/new', lazy: page(() => import('./pages/workouts/WorkoutLoggerPage'), 'WorkoutLoggerPage') },
              { path: '/workouts/:id', lazy: page(workouts, 'WorkoutDetailsPage') },
              { path: '/friends', lazy: page(() => import('./pages/friends/FriendsPage'), 'FriendsPage') },
              { path: '/friends/:friendId/workouts/:sessionId', lazy: page(() => import('./pages/friends/FriendWorkoutPage'), 'FriendWorkoutPage') },
              { path: '/friends/:friendId/progress', lazy: page(() => import('./pages/friends/FriendProgressPage'), 'FriendProgressPage') },
              { path: '/progress', lazy: page(() => import('./pages/progress/ProgressPage'), 'ProgressPage') },
              { path: '/progress/track', lazy: page(() => import('./pages/progress/TrackExercisePage'), 'TrackExercisePage') },
              { path: '/exercises', lazy: page(() => import('./pages/ExercisesPage'), 'ExercisesPage') },
              { path: '/nutrition', lazy: page(() => import('./pages/NutritionPage'), 'NutritionPage') },
              { path: '/statistics', lazy: page(statistics, 'StatisticsPage') },
              { path: '/statistics/records', lazy: page(statistics, 'RecordsPage') },
              { path: '/awards', lazy: page(statistics, 'AwardsPage') },
              { path: '/profile', lazy: page(() => import('./pages/profile/ProfilePage'), 'ProfilePage') },
              { path: '/profile/settings', lazy: page(() => import('./pages/profile/ProfileSettingsPage'), 'ProfileSettingsPage') },
              { path: '/profile/password', lazy: page(() => import('./pages/profile/ChangePasswordPage'), 'ChangePasswordPage') },
            ],
          },
          { path: '*', element: <NotFoundPage /> },
        ],
      },
    ],
  },
];
