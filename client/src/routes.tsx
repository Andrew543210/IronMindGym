import type { RouteObject } from 'react-router';
import { Layout } from './components/Layout';
import { RequireSession } from './components/RequireSession';
import { DashboardPage, FeedNotePage } from './pages/DashboardPage';
import { ExercisesPage } from './pages/ExercisesPage';
import { FriendWorkoutPage, FriendsPage } from './pages/FriendsPage';
import { GoalsPage } from './pages/GoalsPage';
import { NotFoundPage } from './pages/NotFoundPage';
import { NutritionPage } from './pages/NutritionPage';
import { FriendProgressPage, ProgressPage, TrackExercisePage } from './pages/ProgressPages';
import { SetupPage } from './pages/SetupPage';
import { LoginPage, RegisterPage } from './pages/auth/AuthPages';
import { WorkoutLoggerPage } from './pages/workouts/WorkoutLoggerPage';
import { WorkoutDetailsPage, WorkoutHistoryPage } from './pages/workouts/WorkoutPages';

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
          { path: '/goals', element: <GoalsPage /> },
          { path: '/workouts', element: <WorkoutHistoryPage /> },
          { path: '/workouts/new', element: <WorkoutLoggerPage /> },
          { path: '/workouts/:id', element: <WorkoutDetailsPage /> },
          { path: '/friends', element: <FriendsPage /> },
          { path: '/friends/:friendId/workouts/:sessionId', element: <FriendWorkoutPage /> },
          { path: '/friends/:friendId/progress', element: <FriendProgressPage /> },
          { path: '/progress', element: <ProgressPage /> },
          { path: '/progress/track', element: <TrackExercisePage /> },
          { path: '/exercises', element: <ExercisesPage /> },
          { path: '/nutrition', element: <NutritionPage /> },
        ],
      },
      { path: '*', element: <NotFoundPage /> },
    ],
  },
];
