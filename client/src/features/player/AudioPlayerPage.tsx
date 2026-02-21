import { useCallback, useEffect, useRef, useState } from 'react';
import { useLocation, useNavigate, useParams } from 'react-router-dom';
import {
  Alert,
  Box,
  Button,
  Card,
  CardMedia,
  Chip,
  CircularProgress,
  Container,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  IconButton,
  List,
  ListItem,
  ListItemButton,
  ListItemIcon,
  ListItemText,
  Slider,
  Stack,
  TextField,
  ToggleButton,
  ToggleButtonGroup,
  Tooltip,
  Typography,
} from '@mui/material';
import ArrowBackIcon from '@mui/icons-material/ArrowBack';
import PlayArrowIcon from '@mui/icons-material/PlayArrow';
import PauseIcon from '@mui/icons-material/Pause';
import Replay30Icon from '@mui/icons-material/Replay30';
import Forward30Icon from '@mui/icons-material/Forward30';
import BookmarkAddIcon from '@mui/icons-material/BookmarkAdd';
import BookmarkIcon from '@mui/icons-material/Bookmark';
import DeleteIcon from '@mui/icons-material/Delete';
import SpeedIcon from '@mui/icons-material/Speed';
import LibraryMusicIcon from '@mui/icons-material/LibraryMusic';
import { useAppDispatch, useAppSelector } from '../../app/hooks';
import {
  clearPlayerState,
  createBookmarkAsync,
  deleteBookmarkAsync,
  fetchPlayerState,
  saveProgressAsync,
} from './playerSlice';
import { PLAYBACK_SPEEDS, SKIP_SECONDS } from './constants';

/** Convert seconds to mm:ss or hh:mm:ss */
const formatTime = (seconds: number): string => {
  const s = Math.max(0, Math.floor(seconds));
  const h = Math.floor(s / 3600);
  const m = Math.floor((s % 3600) / 60);
  const sec = s % 60;
  const pad = (n: number) => n.toString().padStart(2, '0');
  return h > 0 ? `${h}:${pad(m)}:${pad(sec)}` : `${m}:${pad(sec)}`;
};

const resolveUrl = (path: string | null): string | null => {
  if (!path) return null;
  if (path.startsWith('http://') || path.startsWith('https://')) return path;
  const baseUrl = import.meta.env.VITE_API_URL || 'https://localhost:7196';
  return `${baseUrl}${path}`;
};

/** Interval (ms) between auto-saves of listening progress */
const PROGRESS_SAVE_INTERVAL = 15_000;

export default function AudioPlayerPage() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const location = useLocation();
  const dispatch = useAppDispatch();
  const { playerState, isLoading, error } = useAppSelector((s) => s.player);

  // Audio element ref
  const audioRef = useRef<HTMLAudioElement | null>(null);

  // Local UI state
  const [isPlaying, setIsPlaying] = useState(false);
  const [currentTime, setCurrentTime] = useState(0);
  const [duration, setDuration] = useState(0);
  const [playbackRate, setPlaybackRate] = useState(1);
  const [isSeeking, setIsSeeking] = useState(false);
  const [bookmarkDialogOpen, setBookmarkDialogOpen] = useState(false);
  const [bookmarkLabel, setBookmarkLabel] = useState('');
  const [showBookmarks, setShowBookmarks] = useState(false);
  const backTarget = (location.state as { from?: string } | null)?.from ?? '/';

  // Ref to latest currentTime so we can read it in cleanup / save without re-renders
  const currentTimeRef = useRef(0);
  currentTimeRef.current = currentTime;

  // --- Load player state ---
  useEffect(() => {
    if (id) {
      void dispatch(fetchPlayerState(id));
    }
    return () => {
      dispatch(clearPlayerState());
    };
  }, [dispatch, id]);

  // --- Persist progress periodically ---
  useEffect(() => {
    if (!id) return;

    const interval = setInterval(() => {
      if (currentTimeRef.current > 0) {
        void dispatch(
          saveProgressAsync({
            audiobookId: id,
            positionSeconds: currentTimeRef.current,
            totalDurationSeconds: duration > 0 ? duration : undefined,
          }),
        );
      }
    }, PROGRESS_SAVE_INTERVAL);

    return () => clearInterval(interval);
  }, [dispatch, id]);

  // --- Save progress on unmount / back navigation ---
  useEffect(() => {
    const audiobookId = id;
    return () => {
      if (audiobookId && currentTimeRef.current > 0) {
        void dispatch(
          saveProgressAsync({
            audiobookId,
            positionSeconds: currentTimeRef.current,
            totalDurationSeconds: duration > 0 ? duration : undefined,
          }),
        );
      }
    };
    // Only run cleanup on unmount
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [dispatch, duration, id]);

  // --- Audio element event handlers ---
  const handleLoadedMetadata = useCallback(() => {
    const audio = audioRef.current;
    if (!audio) return;
    setDuration(audio.duration);

    // Resume from last position
    if (playerState && playerState.lastPositionSeconds > 0) {
      audio.currentTime = playerState.lastPositionSeconds;
      setCurrentTime(playerState.lastPositionSeconds);
    }
  }, [playerState]);

  const handleTimeUpdate = useCallback(() => {
    if (!isSeeking && audioRef.current) {
      setCurrentTime(audioRef.current.currentTime);
    }
  }, [isSeeking]);

  const handleEnded = useCallback(() => {
    setIsPlaying(false);
    if (id) {
      void dispatch(
        saveProgressAsync({
          audiobookId: id,
          positionSeconds: audioRef.current?.duration ?? 0,
          totalDurationSeconds: audioRef.current?.duration,
        }),
      );
    }
  }, [dispatch, id]);

  // --- Playback controls ---
  const togglePlay = () => {
    const audio = audioRef.current;
    if (!audio) return;
    if (isPlaying) {
      audio.pause();
      setIsPlaying(false);
      if (id) {
        void dispatch(
          saveProgressAsync({
            audiobookId: id,
            positionSeconds: audio.currentTime,
            totalDurationSeconds: audio.duration > 0 ? audio.duration : undefined,
          }),
        );
      }
    } else {
      void audio.play();
      setIsPlaying(true);
    }
  };

  const handleRewind = () => {
    const audio = audioRef.current;
    if (!audio) return;
    audio.currentTime = Math.max(0, audio.currentTime - SKIP_SECONDS);
    setCurrentTime(audio.currentTime);
  };

  const handleForward = () => {
    const audio = audioRef.current;
    if (!audio) return;
    audio.currentTime = Math.min(audio.duration, audio.currentTime + SKIP_SECONDS);
    setCurrentTime(audio.currentTime);
  };

  const handleSliderChange = (_: unknown, value: number | number[]) => {
    const newTime = Array.isArray(value) ? value[0] : value;
    setCurrentTime(newTime);
    setIsSeeking(true);
  };

  const handleSliderCommitted = (_: unknown, value: number | number[]) => {
    const newTime = Array.isArray(value) ? value[0] : value;
    if (audioRef.current) {
      audioRef.current.currentTime = newTime;
    }
    setIsSeeking(false);
  };

  const handleSpeedChange = (_: unknown, newSpeed: number | null) => {
    if (newSpeed === null) return;
    setPlaybackRate(newSpeed);
    if (audioRef.current) {
      audioRef.current.playbackRate = newSpeed;
    }
  };

  // --- Bookmark handlers ---
  const openBookmarkDialog = () => {
    setBookmarkLabel('');
    setBookmarkDialogOpen(true);
  };

  const submitBookmark = async () => {
    if (!id) return;
    await dispatch(
      createBookmarkAsync({
        audiobookId: id,
        positionSeconds: currentTimeRef.current,
        label: bookmarkLabel.trim() || undefined,
      }),
    );
    setBookmarkDialogOpen(false);
  };

  const handleDeleteBookmark = (bookmarkId: string) => {
    if (!id) return;
    void dispatch(deleteBookmarkAsync({ audiobookId: id, bookmarkId }));
  };

  const jumpToBookmark = (positionSeconds: number) => {
    if (audioRef.current) {
      audioRef.current.currentTime = positionSeconds;
      setCurrentTime(positionSeconds);
    }
  };

  // --- Render ---
  if (isLoading) {
    return (
      <Container maxWidth="sm" sx={{ py: 6 }}>
        <Box sx={{ display: 'flex', justifyContent: 'center' }}>
          <CircularProgress />
        </Box>
      </Container>
    );
  }

  if (error) {
    return (
      <Container maxWidth="sm" sx={{ py: 6 }}>
        <Alert severity="error" sx={{ mb: 2 }}>
          {error}
        </Alert>
        <Button startIcon={<ArrowBackIcon />} onClick={() => navigate(backTarget)}>
          Back to Library
        </Button>
      </Container>
    );
  }

  if (!playerState) {
    return null;
  }

  const audioSrc = resolveUrl(playerState.audioFileUrl);
  const coverSrc = resolveUrl(playerState.coverImageUrl);
  const progressPercent = duration > 0 ? (currentTime / duration) * 100 : 0;

  return (
    <Container maxWidth="sm" sx={{ py: 4 }}>
      {/* Hidden audio element */}
      {audioSrc && (
        <audio
          ref={audioRef}
          src={audioSrc}
          preload="metadata"
          onLoadedMetadata={handleLoadedMetadata}
          onTimeUpdate={handleTimeUpdate}
          onEnded={handleEnded}
        />
      )}

      {/* Back button */}
      <Button startIcon={<ArrowBackIcon />} onClick={() => navigate(backTarget)} sx={{ mb: 2 }}>
        Back to Library
      </Button>

      {/* Cover Art */}
      <Card elevation={3} sx={{ mb: 3, borderRadius: 3, overflow: 'hidden' }}>
        {coverSrc ? (
          <CardMedia
            component="img"
            image={coverSrc}
            alt={`${playerState.title} cover`}
            sx={{ width: '100%', maxHeight: 360, objectFit: 'cover' }}
          />
        ) : (
          <Box
            sx={{
              height: 260,
              display: 'flex',
              alignItems: 'center',
              justifyContent: 'center',
              bgcolor: 'action.hover',
            }}
          >
            <LibraryMusicIcon sx={{ fontSize: 80, color: 'action.disabled' }} />
          </Box>
        )}
      </Card>

      {/* Title & Author */}
      <Typography variant="h5" fontWeight={700} textAlign="center" gutterBottom>
        {playerState.title}
      </Typography>
      <Typography variant="body1" color="text.secondary" textAlign="center" sx={{ mb: 1 }}>
        {playerState.author}
      </Typography>
      {playerState.genre && (
        <Box sx={{ display: 'flex', justifyContent: 'center', mb: 2 }}>
          <Chip label={playerState.genre} size="small" variant="outlined" />
        </Box>
      )}

      {/* Progress Slider */}
      <Box sx={{ px: 1 }}>
        <Slider
          value={currentTime}
          min={0}
          max={duration || 1}
          step={1}
          onChange={handleSliderChange}
          onChangeCommitted={handleSliderCommitted}
          aria-label="Playback position"
          sx={{ color: 'primary.main' }}
        />
        <Stack direction="row" justifyContent="space-between">
          <Typography variant="caption" color="text.secondary">
            {formatTime(currentTime)}
          </Typography>
          <Typography variant="caption" color="text.secondary">
            {formatTime(duration)}
          </Typography>
        </Stack>
        <Typography variant="caption" color="text.secondary" sx={{ display: 'block', textAlign: 'center' }}>
          {progressPercent.toFixed(1)}% listened
        </Typography>
      </Box>

      {/* Playback Controls */}
      <Stack direction="row" justifyContent="center" alignItems="center" spacing={2} sx={{ my: 2 }}>
        <Tooltip title={`Rewind ${SKIP_SECONDS}s`}>
          <IconButton onClick={handleRewind} size="large" color="primary">
            <Replay30Icon fontSize="large" />
          </IconButton>
        </Tooltip>

        <IconButton
          onClick={togglePlay}
          size="large"
          color="primary"
          sx={{
            bgcolor: 'primary.main',
            color: 'white',
            width: 64,
            height: 64,
            '&:hover': { bgcolor: 'primary.dark' },
          }}
          disabled={!audioSrc}
        >
          {isPlaying ? <PauseIcon fontSize="large" /> : <PlayArrowIcon fontSize="large" />}
        </IconButton>

        <Tooltip title={`Forward ${SKIP_SECONDS}s`}>
          <IconButton onClick={handleForward} size="large" color="primary">
            <Forward30Icon fontSize="large" />
          </IconButton>
        </Tooltip>
      </Stack>

      {/* Playback Speed */}
      <Box sx={{ display: 'flex', alignItems: 'center', justifyContent: 'center', mb: 2 }}>
        <SpeedIcon fontSize="small" sx={{ mr: 1, color: 'text.secondary' }} />
        <ToggleButtonGroup
          value={playbackRate}
          exclusive
          onChange={handleSpeedChange}
          size="small"
          aria-label="Playback speed"
        >
          {PLAYBACK_SPEEDS.map((speed) => (
            <ToggleButton key={speed} value={speed} sx={{ px: 1.5 }}>
              {speed}x
            </ToggleButton>
          ))}
        </ToggleButtonGroup>
      </Box>

      {/* Bookmark Actions */}
      <Stack direction="row" justifyContent="center" spacing={1} sx={{ mb: 2 }}>
        <Button
          variant="outlined"
          size="small"
          startIcon={<BookmarkAddIcon />}
          onClick={openBookmarkDialog}
          disabled={!audioSrc}
        >
          Add Bookmark
        </Button>
        <Button
          variant={showBookmarks ? 'contained' : 'outlined'}
          size="small"
          startIcon={<BookmarkIcon />}
          onClick={() => setShowBookmarks(!showBookmarks)}
        >
          Bookmarks ({playerState.bookmarks.length})
        </Button>
      </Stack>

      {/* Bookmarks List */}
      {showBookmarks && (
        <Card variant="outlined" sx={{ mb: 2 }}>
          {playerState.bookmarks.length === 0 ? (
            <Box sx={{ p: 2, textAlign: 'center' }}>
              <Typography variant="body2" color="text.secondary">
                No bookmarks yet. Add one at the current position.
              </Typography>
            </Box>
          ) : (
            <List dense disablePadding>
              {playerState.bookmarks.map((bookmark) => (
                <ListItem
                  key={bookmark.id}
                  disablePadding
                  secondaryAction={
                    <IconButton
                      edge="end"
                      size="small"
                      aria-label="delete bookmark"
                      onClick={() => handleDeleteBookmark(bookmark.id)}
                    >
                      <DeleteIcon fontSize="small" />
                    </IconButton>
                  }
                >
                  <ListItemButton onClick={() => jumpToBookmark(bookmark.positionSeconds)}>
                    <ListItemIcon sx={{ minWidth: 36 }}>
                      <BookmarkIcon fontSize="small" color="primary" />
                    </ListItemIcon>
                    <ListItemText
                      primary={bookmark.label || `Bookmark at ${formatTime(bookmark.positionSeconds)}`}
                      secondary={formatTime(bookmark.positionSeconds)}
                    />
                  </ListItemButton>
                </ListItem>
              ))}
            </List>
          )}
        </Card>
      )}

      {/* Add Bookmark Dialog */}
      <Dialog open={bookmarkDialogOpen} onClose={() => setBookmarkDialogOpen(false)} fullWidth maxWidth="xs">
        <DialogTitle>Add Bookmark</DialogTitle>
        <DialogContent>
          <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
            Position: {formatTime(currentTimeRef.current)}
          </Typography>
          <TextField
            label="Label (optional)"
            value={bookmarkLabel}
            onChange={(e) => setBookmarkLabel(e.target.value)}
            fullWidth
            size="small"
            autoFocus
            placeholder="e.g. Interesting quote"
          />
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setBookmarkDialogOpen(false)}>Cancel</Button>
          <Button variant="contained" onClick={submitBookmark}>
            Save
          </Button>
        </DialogActions>
      </Dialog>
    </Container>
  );
}
