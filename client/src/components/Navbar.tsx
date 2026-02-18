import {
  AppBar,
  Toolbar,
  Typography,
  Button,
  Box,
  IconButton,
} from '@mui/material';
import { Headphones } from '@mui/icons-material';
import { useNavigate } from 'react-router-dom';
import { useAppDispatch, useAppSelector } from '../app/hooks';
import { logoutAsync } from '../features/auth/authSlice';

export default function Navbar() {
  const dispatch = useAppDispatch();
  const navigate = useNavigate();
  const { isAuthenticated, user } = useAppSelector((state) => state.auth);

  const handleLogout = async () => {
    await dispatch(logoutAsync());
    navigate('/login');
  };

  return (
    <AppBar position="static" elevation={0}>
      <Toolbar>
        <IconButton
          edge="start"
          color="inherit"
          aria-label="home"
          onClick={() => navigate('/')}
          sx={{ mr: 1 }}
        >
          <Headphones />
        </IconButton>
        <Typography
          variant="h6"
          component="div"
          sx={{ cursor: 'pointer' }}
          onClick={() => navigate('/')}
        >
          Audivo
        </Typography>
        <Box sx={{ flexGrow: 1 }} />
        {isAuthenticated ? (
          <Box sx={{ display: 'flex', alignItems: 'center', gap: 2 }}>
            <Typography variant="body2">
              {user?.firstName} {user?.lastName}
            </Typography>
            <Button color="inherit" onClick={handleLogout}>
              Sign Out
            </Button>
          </Box>
        ) : (
          <Box sx={{ display: 'flex', gap: 1 }}>
            <Button color="inherit" onClick={() => navigate('/login')}>
              Sign In
            </Button>
            <Button color="inherit" variant="outlined" onClick={() => navigate('/register')}>
              Sign Up
            </Button>
          </Box>
        )}
      </Toolbar>
    </AppBar>
  );
}
