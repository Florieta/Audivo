import {
  AppBar,
  Toolbar,
  Typography,
  Button,
  Box,
  IconButton,
  Avatar,
  Tooltip,
} from '@mui/material';
import { deepPurple } from '@mui/material/colors';
import { Headphones, LibraryBooks } from '@mui/icons-material';
import { useNavigate } from 'react-router-dom';
import { useAppDispatch, useAppSelector } from '../app/hooks';
import { logoutAsync } from '../features/auth/authSlice';

export default function Navbar() {
  const dispatch = useAppDispatch();
  const navigate = useNavigate();
  const { isAuthenticated, user } = useAppSelector((state) => state.auth);

  const initials = `${user?.firstName?.[0] ?? ''}${user?.lastName?.[0] ?? ''}`.toUpperCase() || 'U';

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
        {isAuthenticated && (
          <Button
            color="inherit"
            startIcon={<LibraryBooks />}
            onClick={() => navigate('/library')}
            sx={{ ml: 2 }}
          >
            My Library
          </Button>
        )}
        <Box sx={{ flexGrow: 1 }} />
        {isAuthenticated ? (
          <Box sx={{ display: 'flex', alignItems: 'center', gap: 2 }}>
            <Tooltip title={`${user?.firstName ?? ''} ${user?.lastName ?? ''}`.trim() || user?.email || 'User'}>
              <Avatar sx={{ width: 32, height: 32, bgcolor: deepPurple[500], fontSize: 14 }}>
                {initials}
              </Avatar>
            </Tooltip>
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
