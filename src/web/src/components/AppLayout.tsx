import { AppBar, Box, Button, Container, Toolbar, Typography } from '@mui/material'
import { Outlet } from 'react-router'
import { useGetCurrentUser } from '../api/generated/identity/identity'
import { useAuth } from '../auth/AuthContext'

export function AppLayout() {
  const { signOut } = useAuth()
  const me = useGetCurrentUser({ query: { select: (r) => r.data } })

  return (
    <>
      <AppBar position="static">
        <Toolbar sx={{ gap: 2, flexWrap: 'wrap' }}>
          <Typography variant="h6" component="span" sx={{ flexGrow: 1 }}>
            TenancyManager
          </Typography>
          {me.data && (
            <Box sx={{ textAlign: 'right' }} data-testid="current-user">
              <Typography variant="body2">{me.data.agency.name}</Typography>
              <Typography variant="caption">
                {me.data.displayName} · {me.data.roles.join(', ')}
              </Typography>
            </Box>
          )}
          <Button color="inherit" onClick={() => void signOut()}>
            Sign out
          </Button>
        </Toolbar>
      </AppBar>
      <Container component="main" sx={{ py: 3 }}>
        <Outlet />
      </Container>
    </>
  )
}
