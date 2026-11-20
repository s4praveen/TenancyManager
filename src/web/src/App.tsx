import { createBrowserRouter, Navigate, RouterProvider } from 'react-router'
import { useAuth } from './auth/AuthContext'
import { AppLayout } from './components/AppLayout'
import { PropertiesPage } from './pages/PropertiesPage'
import { SignInPage } from './pages/SignInPage'

const router = createBrowserRouter([
  {
    path: '/',
    element: <AppLayout />,
    children: [
      { index: true, element: <Navigate to="/properties" replace /> },
      { path: 'properties', element: <PropertiesPage /> },
    ],
  },
  { path: '*', element: <Navigate to="/" replace /> },
])

export default function App() {
  const { isSignedIn } = useAuth()
  return isSignedIn ? <RouterProvider router={router} /> : <SignInPage />
}
