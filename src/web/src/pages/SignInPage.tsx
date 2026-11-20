import {
  Box,
  Button,
  Card,
  CardContent,
  FormControl,
  InputLabel,
  MenuItem,
  Select,
  Stack,
  TextField,
  Typography,
} from '@mui/material'
import { useState, type FormEvent } from 'react'
import { AGENCY_ROLES, useAuth, type AgencyRole } from '../auth/AuthContext'

export function SignInPage() {
  const { mode, signIn } = useAuth()
  const [user, setUser] = useState('agent@example.test')
  const [role, setRole] = useState<AgencyRole>('Agent')

  const submit = (e: FormEvent) => {
    e.preventDefault()
    void signIn(user, [role])
  }

  return (
    <Box sx={{ display: 'grid', placeItems: 'center', minHeight: '100vh', p: 2 }}>
      <Card sx={{ width: '100%', maxWidth: 420 }}>
        <CardContent>
          <Typography variant="h5" component="h1" gutterBottom>
            TenancyManager
          </Typography>
          {mode === 'entra' ? (
            <Button variant="contained" fullWidth onClick={() => void signIn()}>
              Sign in with Microsoft
            </Button>
          ) : (
            <Stack component="form" spacing={2} onSubmit={submit}>
              <Typography variant="body2" color="text.secondary">
                Development sign-in. Pick who to act as.
              </Typography>
              <TextField label="User" value={user} onChange={(e) => setUser(e.target.value)} required />
              <FormControl>
                <InputLabel id="role-label">Role</InputLabel>
                <Select
                  labelId="role-label"
                  label="Role"
                  value={role}
                  onChange={(e) => setRole(e.target.value as AgencyRole)}
                >
                  {AGENCY_ROLES.map((r) => (
                    <MenuItem key={r} value={r}>
                      {r}
                    </MenuItem>
                  ))}
                </Select>
              </FormControl>
              <Button type="submit" variant="contained">
                Sign in
              </Button>
            </Stack>
          )}
        </CardContent>
      </Card>
    </Box>
  )
}
