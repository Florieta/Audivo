import { Box, Container, Typography } from '@mui/material';
import { useAppSelector } from '../../app/hooks';

export default function AudiobooksPage() {
  const { user } = useAppSelector((state) => state.auth);

  return (
    <Container maxWidth="lg">
      <Box sx={{ mt: 4 }}>
        <Typography variant="h4" gutterBottom>
          Welcome{user ? `, ${user.firstName}` : ''}!
        </Typography>
        <Typography variant="body1" color="text.secondary">
          Your audiobook library will appear here. Start exploring and adding audiobooks to your
          collection.
        </Typography>
      </Box>
    </Container>
  );
}
