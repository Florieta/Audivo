import { useEffect, useMemo, useState, type ChangeEvent, type FormEvent } from 'react';
import {
  Alert,
  Box,
  Button,
  Card,
  CardActions,
  CardContent,
  CardMedia,
  CircularProgress,
  Container,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  Grid,
  LinearProgress,
  IconButton,
  InputAdornment,
  Stack,
  Chip,
  TextField,
  Tooltip,
  Typography,
} from '@mui/material';
import SearchIcon from '@mui/icons-material/Search';
import FavoriteBorderIcon from '@mui/icons-material/FavoriteBorder';
import FavoriteIcon from '@mui/icons-material/Favorite';
import PlayArrowIcon from '@mui/icons-material/PlayArrow';
import EditIcon from '@mui/icons-material/Edit';
import DeleteIcon from '@mui/icons-material/Delete';
import AddIcon from '@mui/icons-material/Add';
import LibraryMusicIcon from '@mui/icons-material/LibraryMusic';
import { useNavigate } from 'react-router-dom';
import { useAppDispatch, useAppSelector } from '../../app/hooks';
import {
  addFavoriteAsync,
  clearLibrarySearch,
  fetchLibrarySections,
  removeFavoriteAsync,
  searchLibraryAudiobooks,
} from './librarySlice';
import { AUDIO_ACCEPT, AUDIO_FORMATS, GENRE_OPTIONS, IMAGE_ACCEPT, IMAGE_FORMATS } from '../audiobooks/constants';
import type { AudiobookFormState, LibraryAudiobook } from '../../types';
import { audiobookService } from '../../services/audiobookService';

const emptyFormState: AudiobookFormState = {
  title: '',
  author: '',
  genre: '',
  coverImage: null,
  audioFile: null,
};

const resolveImageUrl = (path: string | null) => {
  if (!path) {
    return null;
  }

  if (path.startsWith('http://') || path.startsWith('https://')) {
    return path;
  }

  const baseUrl = import.meta.env.VITE_API_URL || 'https://localhost:7196';
  return `${baseUrl}${path}`;
};

const getGenreLabel = (value: string | null) =>
  GENRE_OPTIONS.find((genre) => genre.value === value)?.label ?? value;

const formatDuration = (totalSeconds: number) => {
  const clamped = Math.max(0, Math.floor(totalSeconds));
  const hours = Math.floor(clamped / 3600);
  const minutes = Math.floor((clamped % 3600) / 60);

  if (hours > 0) {
    return `${hours}h ${minutes}m`;
  }

  return `${minutes}m`;
};

function SectionCards({
  title,
  subtitle,
  books,
  onFavoriteToggle,
  onPlay,
  showOwnerActions,
  onEdit,
  onDelete,
}: {
  title?: string;
  subtitle?: string;
  books: LibraryAudiobook[];
  onFavoriteToggle: (book: LibraryAudiobook) => void;
  onPlay: (bookId: string) => void;
  showOwnerActions?: boolean;
  onEdit?: (bookId: string) => void;
  onDelete?: (bookId: string) => void;
}) {
  return (
    <Box sx={{ mt: 4 }}>
      {title && (
        <Typography variant="h5" sx={{ fontWeight: 700 }}>
          {title}
        </Typography>
      )}
      {subtitle && (
        <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
          {subtitle}
        </Typography>
      )}

      {books.length === 0 ? (
        <Box
          sx={{
            py: 5,
            display: 'flex',
            flexDirection: 'column',
            alignItems: 'center',
            gap: 1,
            color: 'text.secondary',
          }}
        >
          <LibraryMusicIcon fontSize="large" />
          <Typography variant="body2">No audiobooks in this section yet.</Typography>
        </Box>
      ) : (
        <Grid container spacing={2}>
          {books.map((book) => (
            <Grid key={book.id} size={{ xs: 12, sm: 6, md: 4 }}>
              <Card sx={{ height: '100%', display: 'flex', flexDirection: 'column' }}>
                {resolveImageUrl(book.coverImageUrl) ? (
                  <CardMedia
                    component="img"
                    height="220"
                    image={resolveImageUrl(book.coverImageUrl)!}
                    alt={`${book.title} cover`}
                  />
                ) : (
                  <Box
                    sx={{
                      height: 220,
                      display: 'flex',
                      alignItems: 'center',
                      justifyContent: 'center',
                      bgcolor: 'action.hover',
                    }}
                  >
                    <LibraryMusicIcon fontSize="large" color="action" />
                  </Box>
                )}
                <CardContent sx={{ flexGrow: 1 }}>
                  <Typography variant="h6" noWrap title={book.title}>
                    {book.title}
                  </Typography>
                  <Typography variant="body2" color="text.secondary" noWrap title={book.author}>
                    {book.author}
                  </Typography>
                  {book.genre && (
                    <Typography variant="body2" color="text.secondary" sx={{ mt: 1 }}>
                      Genre: {getGenreLabel(book.genre)}
                    </Typography>
                  )}

                  <Stack direction="row" spacing={1} sx={{ mt: 1.25 }}>
                    {book.isCompleted ? (
                      <Chip label="Completed" color="success" size="small" />
                    ) : book.progressPercent > 0 ? (
                      <Chip label="In Progress" color="primary" size="small" />
                    ) : (
                      <Chip label="Not Started" size="small" />
                    )}
                  </Stack>

                  <Box sx={{ mt: 1.5 }}>
                    <Typography variant="caption" color="text.secondary">
                      {book.progressPercent.toFixed(1)}% completed
                    </Typography>
                    <LinearProgress
                      variant="determinate"
                      value={book.progressPercent}
                      sx={{ mt: 0.5, height: 8, borderRadius: 999 }}
                    />
                  </Box>

                  <Typography variant="body2" color="text.secondary" sx={{ mt: 1 }}>
                    {formatDuration(book.listenedSeconds)} / {formatDuration(book.totalDurationSeconds)}
                  </Typography>
                  <Typography variant="caption" color="text.secondary">
                    Stats: total listened {formatDuration(book.listenedSeconds)}
                  </Typography>
                </CardContent>
                <CardActions>
                  <Button
                    size="small"
                    variant={book.progressPercent > 0 ? 'contained' : 'outlined'}
                    startIcon={<PlayArrowIcon />}
                    onClick={() => onPlay(book.id)}
                  >
                    {book.progressPercent > 0 && !book.isCompleted ? 'Resume Listening' : 'Play'}
                  </Button>
                  {showOwnerActions && onEdit && onDelete && (
                    <>
                      <Tooltip title="Edit audiobook">
                        <IconButton color="primary" onClick={() => onEdit(book.id)}>
                          <EditIcon />
                        </IconButton>
                      </Tooltip>
                      <Tooltip title="Delete audiobook">
                        <IconButton color="primary" onClick={() => onDelete(book.id)}>
                          <DeleteIcon />
                        </IconButton>
                      </Tooltip>
                    </>
                  )}
                  <Tooltip title={book.isFavorite ? 'Remove from favourites' : 'Add to favourites'}>
                    <IconButton color="primary" onClick={() => onFavoriteToggle(book)}>
                      {book.isFavorite ? <FavoriteIcon /> : <FavoriteBorderIcon />}
                    </IconButton>
                  </Tooltip>
                </CardActions>
              </Card>
            </Grid>
          ))}
        </Grid>
      )}
    </Box>
  );
}

export default function MyLibraryPage() {
  const dispatch = useAppDispatch();
  const navigate = useNavigate();
  const { uploadedBooks, favoriteBooks, searchResults, isLoading, isSearching, error } = useAppSelector(
    (state) => state.library,
  );

  const [searchTerm, setSearchTerm] = useState('');
  const [isFormOpen, setIsFormOpen] = useState(false);
  const [selectedAudiobook, setSelectedAudiobook] = useState<LibraryAudiobook | null>(null);
  const [formState, setFormState] = useState<AudiobookFormState>(emptyFormState);
  const [localError, setLocalError] = useState<string | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [uploadProgress, setUploadProgress] = useState(0);

  useEffect(() => {
    void dispatch(fetchLibrarySections());
  }, [dispatch]);

  useEffect(() => {
    const intervalId = setInterval(() => {
      void dispatch(fetchLibrarySections());
      if (searchTerm.trim()) {
        void dispatch(searchLibraryAudiobooks(searchTerm));
      }
    }, 15000);

    return () => clearInterval(intervalId);
  }, [dispatch, searchTerm]);

  useEffect(() => {
    const timerId = setTimeout(() => {
      if (searchTerm.trim()) {
        void dispatch(searchLibraryAudiobooks(searchTerm));
      } else {
        dispatch(clearLibrarySearch());
      }
    }, 350);

    return () => clearTimeout(timerId);
  }, [dispatch, searchTerm]);

  const onFavoriteToggle = (book: LibraryAudiobook) => {
    if (book.isFavorite) {
      void dispatch(removeFavoriteAsync(book.id));
      return;
    }

    void dispatch(addFavoriteAsync(book.id));
  };

  const openAddDialog = () => {
    setSelectedAudiobook(null);
    setFormState(emptyFormState);
    setLocalError(null);
    setIsFormOpen(true);
  };

  const openEditDialog = (bookId: string) => {
    const found = uploadedBooks.find((book) => book.id === bookId);
    if (!found) {
      return;
    }

    setSelectedAudiobook(found);
    setFormState({
      title: found.title,
      author: found.author,
      genre: found.genre ?? '',
      coverImage: null,
      audioFile: null,
    });
    setLocalError(null);
    setIsFormOpen(true);
  };

  const closeFormDialog = () => {
    if (isSubmitting) {
      return;
    }

    setIsFormOpen(false);
    setSelectedAudiobook(null);
    setFormState(emptyFormState);
    setLocalError(null);
    setUploadProgress(0);
  };

  const onFieldChange = (field: keyof AudiobookFormState) => (event: ChangeEvent<HTMLInputElement>) => {
    const value = event.target.value;
    setFormState((prev) => ({ ...prev, [field]: value }));
  };

  const onFileChange = (field: 'coverImage' | 'audioFile') => (event: ChangeEvent<HTMLInputElement>) => {
    const file = event.target.files?.[0] ?? null;
    setFormState((prev) => ({ ...prev, [field]: file }));
  };

  const submitForm = async (event: FormEvent) => {
    event.preventDefault();
    setLocalError(null);

    if (formState.coverImage) {
      const coverExtension = `.${formState.coverImage.name.split('.').pop()?.toLowerCase() ?? ''}`;
      if (!IMAGE_FORMATS.includes(coverExtension as (typeof IMAGE_FORMATS)[number])) {
        setLocalError(`Cover image format is not allowed. Allowed: ${IMAGE_FORMATS.join(', ')}`);
        return;
      }
    }

    if (formState.audioFile) {
      const audioExtension = `.${formState.audioFile.name.split('.').pop()?.toLowerCase() ?? ''}`;
      if (!AUDIO_FORMATS.includes(audioExtension as (typeof AUDIO_FORMATS)[number])) {
        setLocalError(`Audio format is not allowed. Allowed: ${AUDIO_FORMATS.join(', ')}`);
        return;
      }
    }

    setIsSubmitting(true);
    setUploadProgress(0);

    try {
      if (selectedAudiobook) {
        await audiobookService.updateAudiobook(selectedAudiobook.id, formState, setUploadProgress);
      } else {
        await audiobookService.createAudiobook(formState, setUploadProgress);
      }

      await dispatch(fetchLibrarySections());
      if (searchTerm.trim()) {
        await dispatch(searchLibraryAudiobooks(searchTerm));
      }
      closeFormDialog();
    } catch (submitError) {
      setLocalError(submitError instanceof Error ? submitError.message : 'Failed to save audiobook.');
    } finally {
      setIsSubmitting(false);
      setUploadProgress(0);
    }
  };

  const onDeleteUploadedBook = async (bookId: string) => {
    try {
      await audiobookService.deleteAudiobook(bookId);
      await dispatch(fetchLibrarySections());
      if (searchTerm.trim()) {
        await dispatch(searchLibraryAudiobooks(searchTerm));
      }
    } catch (deleteError) {
      setLocalError(deleteError instanceof Error ? deleteError.message : 'Failed to delete audiobook.');
    }
  };

  const hasSearch = useMemo(() => searchTerm.trim().length > 0, [searchTerm]);

  if (isLoading) {
    return (
      <Container maxWidth="lg" sx={{ py: 6 }}>
        <Box sx={{ display: 'flex', justifyContent: 'center' }}>
          <CircularProgress />
        </Box>
      </Container>
    );
  }

  return (
    <Container maxWidth="lg" sx={{ py: 4 }}>
      <Stack direction="row" justifyContent="space-between" alignItems="center" sx={{ mb: 2 }}>
        <Box>
          <Typography variant="h4" sx={{ fontWeight: 700, color: 'primary.main', mb: 1 }}>
            My Library
          </Typography>
          <Typography variant="body1" color="text.secondary">
            Manage your uploads and favourites.
          </Typography>
        </Box>
        <Button variant="contained" startIcon={<AddIcon />} onClick={openAddDialog}>
          Add Audiobook
        </Button>
      </Stack>

      <TextField
        fullWidth
        label="Search by title, author, or genre"
        value={searchTerm}
        onChange={(e) => setSearchTerm(e.target.value)}
        InputProps={{
          startAdornment: (
            <InputAdornment position="start">
              <SearchIcon />
            </InputAdornment>
          ),
        }}
      />

      {error && (
        <Alert severity="error" sx={{ mt: 2 }}>
          {error}
        </Alert>
      )}

      {localError && (
        <Alert severity="error" sx={{ mt: 2 }}>
          {localError}
        </Alert>
      )}

      {hasSearch && (
        <Box sx={{ mt: 4 }}>
          <Typography variant="h5" sx={{ fontWeight: 700 }}>
            Search Results
          </Typography>
          <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
            Matching audiobooks across title, author, and genre.
          </Typography>
          {isSearching ? (
            <Box sx={{ display: 'flex', justifyContent: 'center', py: 4 }}>
              <CircularProgress size={24} />
            </Box>
          ) : (
            <SectionCards
              books={searchResults}
              onFavoriteToggle={onFavoriteToggle}
              onPlay={(bookId) => navigate(`/player/${bookId}`, { state: { from: '/library' } })}
            />
          )}
        </Box>
      )}

      <SectionCards
        title="Uploaded by Me"
        subtitle="All audiobooks you uploaded are kept here."
        books={uploadedBooks}
        onFavoriteToggle={onFavoriteToggle}
        onPlay={(bookId) => navigate(`/player/${bookId}`, { state: { from: '/library' } })}
        showOwnerActions
        onEdit={openEditDialog}
        onDelete={(bookId) => {
          void onDeleteUploadedBook(bookId);
        }}
      />

      <SectionCards
        title="My Favourites"
        subtitle="Favourite books can include your own uploads and books from other users."
        books={favoriteBooks}
        onFavoriteToggle={onFavoriteToggle}
        onPlay={(bookId) => navigate(`/player/${bookId}`, { state: { from: '/library' } })}
      />

      <Dialog open={isFormOpen} onClose={closeFormDialog} fullWidth maxWidth="sm">
        <DialogTitle>{selectedAudiobook ? 'Edit Audiobook' : 'Add Audiobook'}</DialogTitle>
        {isSubmitting && (
          <Box sx={{ px: 3 }}>
            <LinearProgress variant="determinate" value={uploadProgress} sx={{ borderRadius: 1 }} />
            <Typography variant="body2" color="text.secondary" sx={{ mt: 0.5, textAlign: 'center' }}>
              {uploadProgress < 100
                ? `Uploading… ${uploadProgress}%`
                : 'Processing on server…'}
            </Typography>
          </Box>
        )}
        <Box component="form" onSubmit={submitForm}>
          <DialogContent>
            <Stack spacing={2}>
              <TextField
                label="Title"
                value={formState.title}
                onChange={onFieldChange('title')}
                required
                disabled={isSubmitting}
              />
              <TextField
                label="Author"
                value={formState.author}
                onChange={onFieldChange('author')}
                required
                disabled={isSubmitting}
              />
              <TextField
                label="Genre"
                value={formState.genre}
                onChange={onFieldChange('genre')}
                select
                SelectProps={{ native: true }}
                InputLabelProps={{ shrink: true }}
                disabled={isSubmitting}
              >
                <option value="">Select genre</option>
                {GENRE_OPTIONS.map((genre) => (
                  <option key={genre.value} value={genre.value}>
                    {genre.label}
                  </option>
                ))}
              </TextField>

              <TextField
                label="Supported audio formats"
                value={AUDIO_FORMATS.join(', ')}
                InputProps={{ readOnly: true }}
                size="small"
              />
              <TextField
                label="Supported image formats"
                value={IMAGE_FORMATS.join(', ')}
                InputProps={{ readOnly: true }}
                size="small"
              />

              <Stack direction="row" spacing={2} alignItems="center">
                <Button variant="outlined" component="label" disabled={isSubmitting}>
                  Upload Cover Image
                  <input hidden type="file" accept={IMAGE_ACCEPT} onChange={onFileChange('coverImage')} />
                </Button>
                <Typography variant="body2" color="text.secondary">
                  {formState.coverImage?.name
                    ?? (selectedAudiobook ? 'Leave empty to keep current cover' : 'No file selected')}
                </Typography>
              </Stack>

              <Stack direction="row" spacing={2} alignItems="center">
                <Button variant="outlined" component="label" disabled={isSubmitting}>
                  Upload Audio File
                  <input hidden type="file" accept={AUDIO_ACCEPT} onChange={onFileChange('audioFile')} />
                </Button>
                <Typography variant="body2" color="text.secondary">
                  {formState.audioFile?.name
                    ?? (selectedAudiobook ? 'Leave empty to keep current audio' : 'No file selected')}
                </Typography>
              </Stack>
            </Stack>
          </DialogContent>
          <DialogActions>
            <Button onClick={closeFormDialog} disabled={isSubmitting}>Cancel</Button>
            <Button type="submit" variant="contained" disabled={isSubmitting}>
              {isSubmitting ? 'Uploading…' : selectedAudiobook ? 'Save Changes' : 'Upload'}
            </Button>
          </DialogActions>
        </Box>
      </Dialog>
    </Container>
  );
}
