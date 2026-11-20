import {
  Alert,
  Chip,
  CircularProgress,
  Paper,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  Typography,
} from '@mui/material'
import { useListProperties } from '../api/generated/properties/properties'
import type { Jurisdiction } from '../api/generated/model'

const jurisdictionColour: Record<Jurisdiction, 'primary' | 'error' | 'success'> = {
  England: 'primary',
  Wales: 'error',
  Scotland: 'success',
}

export function PropertiesPage() {
  const properties = useListProperties({ query: { select: (r) => r.data } })

  return (
    <>
      <Typography variant="h4" component="h1" gutterBottom>
        Properties
      </Typography>

      {properties.isPending && <CircularProgress aria-label="Loading properties" />}
      {properties.isError && <Alert severity="error">Could not load properties.</Alert>}

      {properties.data && (
        <TableContainer component={Paper}>
          <Table aria-label="Properties">
            <TableHead>
              <TableRow>
                <TableCell>Address</TableCell>
                <TableCell>Postcode</TableCell>
                <TableCell>Jurisdiction</TableCell>
                <TableCell>Landlord</TableCell>
                <TableCell align="right">Bedrooms</TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {properties.data.map((p) => (
                <TableRow key={p.id}>
                  <TableCell>{[p.line1, p.line2, p.town].filter(Boolean).join(', ')}</TableCell>
                  <TableCell>{p.postcode}</TableCell>
                  <TableCell>
                    <Chip size="small" label={p.jurisdiction} color={jurisdictionColour[p.jurisdiction]} />
                  </TableCell>
                  <TableCell>{p.landlordName}</TableCell>
                  <TableCell align="right">{p.bedrooms}</TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </TableContainer>
      )}
    </>
  )
}
