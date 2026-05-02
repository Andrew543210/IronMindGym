import { useState } from 'react';
import { createBrowserRouter } from 'react-router';
import { RouterProvider } from 'react-router/dom';
import { routes } from './routes';
import { SessionProvider } from './SessionProvider';

export function App() {
  const [router] = useState(() => createBrowserRouter(routes));
  return (
    <SessionProvider>
      <RouterProvider router={router} />
    </SessionProvider>
  );
}
