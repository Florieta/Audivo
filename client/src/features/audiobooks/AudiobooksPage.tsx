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
  IconButton,
  Stack,
  TextField,
  Typography,
} from '@mui/material';
import EditIcon from '@mui/icons-material/Edit';
import DeleteIcon from '@mui/icons-material/Delete';
import AddIcon from '@mui/icons-material/Add';
import LibraryMusicIcon from '@mui/icons-material/LibraryMusic';
import { useAppDispatch, useAppSelector } from '../../app/hooks';
import {
  clearAudiobookErrors,
  createAudiobookAsync,
  deleteAudiobookAsync,
  fetchMyAudiobooks,
  updateAudiobookAsync,
} from './audiobooksSlice';
import type { Audiobook, AudiobookFormState } from '../../types';
import { AUDIO_ACCEPT, AUDIO_FORMATS, GENRE_OPTIONS, IMAGE_ACCEPT, IMAGE_FORMATS } from './constants';

const emptyFormState: AudiobookFormState = {
  title: '',
  author: '',
  genre: '',
  coverImage: null,
  audioFile: null,
};

export default function AudiobooksPage() {
  const dispatch = useAppDispatch();
  const { user } = useAppSelector((state) => state.auth);
  const { items, isLoading, error, validationErrors } = useAppSelector((state) => state.audiobooks);

  const [isFormOpen, setIsFormOpen] = useState(false);
  const [isDeleteOpen, setIsDeleteOpen] = useState(false);
  const [selectedAudiobook, setSelectedAudiobook] = useState<Audiobook | null>(null);
  const [formState, setFormState] = useState<AudiobookFormState>(emptyFormState);
  const [localFileError, setLocalFileError] = useState<string | null>(null);

  useEffect(() => {
    void dispatch(fetchMyAudiobooks());
  }, [dispatch]);

  const isEditing = Boolean(selectedAudiobook);

  const titleError = useMemo(() => validationErrors?.title?.[0] ?? null, [validationErrors]);
  const authorError = useMemo(() => validationErrors?.author?.[0] ?? null, [validationErrors]);
  const genreError = useMemo(() => validationErrors?.genre?.[0] ?? null, [validationErrors]);
  const coverError = useMemo(() => validationErrors?.coverImage?.[0] ?? null, [validationErrors]);
  const audioError = useMemo(() => validationErrors?.audioFile?.[0] ?? null, [validationErrors]);

  const openAddDialog = () => {
    setSelectedAudiobook(null);
    setFormState(emptyFormState);
    setLocalFileError(null);
    dispatch(clearAudiobookErrors());
    setIsFormOpen(true);
  };

  const openEditDialog = (audiobook: Audiobook) => {
    setSelectedAudiobook(audiobook);
    setFormState({
      title: audiobook.title,
      author: audiobook.author,
      genre: audiobook.genre ?? '',
      coverImage: null,
      audioFile: null,
    });
    setLocalFileError(null);
    dispatch(clearAudiobookErrors());
    setIsFormOpen(true);
  };

  const closeFormDialog = () => {
    setIsFormOpen(false);
    setSelectedAudiobook(null);
    setFormState(emptyFormState);
    setLocalFileError(null);
  };

  const openDeleteDialog = (audiobook: Audiobook) => {
    setSelectedAudiobook(audiobook);
    setIsDeleteOpen(true);
  };

  const closeDeleteDialog = () => {
    setIsDeleteOpen(false);
    setSelectedAudiobook(null);
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
    dispatch(clearAudiobookErrors());
    setLocalFileError(null);

    if (formState.coverImage) {
      const coverExtension = `.${formState.coverImage.name.split('.').pop()?.toLowerCase() ?? ''}`;
      if (!IMAGE_FORMATS.includes(coverExtension as (typeof IMAGE_FORMATS)[number])) {
        setLocalFileError(`Cover image format is not allowed. Allowed: ${IMAGE_FORMATS.join(', ')}`);
        return;
      }
    }

    if (formState.audioFile) {
      const audioExtension = `.${formState.audioFile.name.split('.').pop()?.toLowerCase() ?? ''}`;
      if (!AUDIO_FORMATS.includes(audioExtension as (typeof AUDIO_FORMATS)[number])) {
        setLocalFileError(`Audio format is not allowed. Allowed: ${AUDIO_FORMATS.join(', ')}`);
        return;
      }
    }

    const result = isEditing && selectedAudiobook
      ? await dispatch(updateAudiobookAsync({ id: selectedAudiobook.id, payload: formState }))
      : await dispatch(createAudiobookAsync(formState));

    if (updateAudiobookAsync.fulfilled.match(result) || createAudiobookAsync.fulfilled.match(result)) {
      closeFormDialog();
    }
  };

  const confirmDelete = async () => {
    if (!selectedAudiobook) {
      return;
    }

    const result = await dispatch(deleteAudiobookAsync(selectedAudiobook.id));
    if (deleteAudiobookAsync.fulfilled.match(result)) {
      closeDeleteDialog();
    }
  };

  const resolveImageUrl = (path: string | null) => {
    if (!path) {
      return null;
    }

    if (path.startsWith('http://') || path.startsWith('https://')) {
      return path;
    }

    const baseUrl = import.meta.env.VITE_API_URL || 'https://localhost:7001';
    return `${baseUrl}${path}`;
  };

  const getGenreLabel = (value: string | null) =>
    GENRE_OPTIONS.find((genre) => genre.value === value)?.label ?? value;

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
      <Stack direction="row" justifyContent="space-between" alignItems="center" sx={{ mb: 3 }}>
        <Box>
          <Typography
            variant="h4"
            gutterBottom
            sx={{
              fontWeight: 700,
              color: 'primary.main',
              letterSpacing: 0.3,
            }}
          >
            Welcome{user ? `, ${user.firstName}` : ''}
          </Typography>
          <Typography variant="body1" color="text.secondary">
            Your uploaded audiobooks.
          </Typography>
        </Box>
        <Button variant="contained" startIcon={<AddIcon />} onClick={openAddDialog}>
          Add Audiobook
        </Button>
      </Stack>

      {error && (
        <Alert severity="error" sx={{ mb: 2 }}>
          {error}
        </Alert>
      )}

      {items.length === 0 ? (
        <Box
          sx={{
            py: 8,
            display: 'flex',
            flexDirection: 'column',
            alignItems: 'center',
            gap: 1,
            color: 'text.secondary',
          }}
        >
          <LibraryMusicIcon fontSize="large" />
          <Typography variant="h6">No audiobooks yet</Typography>
          <Typography variant="body2">Click "Add Audiobook" to upload your first one.</Typography>
        </Box>
      ) : (
        <Grid container spacing={2}>
          {items.map((audiobook) => (
            <Grid key={audiobook.id} size={{ xs: 12, sm: 6, md: 4 }}>
              <Card sx={{ height: '100%', display: 'flex', flexDirection: 'column' }}>
                {resolveImageUrl(audiobook.coverImageUrl) ? (
                  <CardMedia
                    component="img"
                    height="220"
                    image={resolveImageUrl(audiobook.coverImageUrl)!}
                    alt={`${audiobook.title} cover`}
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
                  <Typography variant="h6" noWrap title={audiobook.title}>
                    {audiobook.title}
                  </Typography>
                  <Typography variant="body2" color="text.secondary" noWrap title={audiobook.author}>
                    {audiobook.author}
                  </Typography>
                  {audiobook.genre && (
                    <Typography variant="body2" color="text.secondary" sx={{ mt: 1 }}>
                      Genre: {getGenreLabel(audiobook.genre)}
                    </Typography>
                  )}
                </CardContent>
                <CardActions>
                  <IconButton color="primary" aria-label="edit" onClick={() => openEditDialog(audiobook)}>
                    <EditIcon />
                  </IconButton>
                  <IconButton color="primary" aria-label="delete" onClick={() => openDeleteDialog(audiobook)}>
                    <DeleteIcon />
                  </IconButton>
                </CardActions>
              </Card>
            </Grid>
          ))}
        </Grid>
      )}

      <Dialog open={isFormOpen} onClose={closeFormDialog} fullWidth maxWidth="sm">
        <DialogTitle>{isEditing ? 'Edit Audiobook' : 'Add Audiobook'}</DialogTitle>
        <Box component="form" onSubmit={submitForm}>
          <DialogContent>
            <Stack spacing={2}>
              <TextField
                label="Title"
                value={formState.title}
                onChange={onFieldChange('title')}
                required
                error={Boolean(titleError)}
                helperText={titleError ?? ''}
              />
              <TextField
                label="Author"
                value={formState.author}
                onChange={onFieldChange('author')}
                required
                error={Boolean(authorError)}
                helperText={authorError ?? ''}
              />
              <TextField
                label="Genre"
                value={formState.genre}
                onChange={onFieldChange('genre')}
                select
                SelectProps={{ native: true }}
                InputLabelProps={{ shrink: true }}
                error={Boolean(genreError)}
                helperText={genreError ?? ''}
              >
                <option value="">Select genre</option>
                {GENRE_OPTIONS.map((genre) => (
                  <option key={genre.value} value={genre.value}>
                    {genre.label}
                  </option>
                ))}
              </TextField>

              {localFileError && <Alert severity="error">{localFileError}</Alert>}

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
                <Button variant="outlined" component="label">
                  Upload Cover Image
                  <input hidden type="file" accept={IMAGE_ACCEPT} onChange={onFileChange('coverImage')} />
                </Button>
                <Typography variant="body2" color="text.secondary">
                  {formState.coverImage?.name ?? (isEditing ? 'Leave empty to keep current cover' : 'No file selected')}
                </Typography>
              </Stack>
              {coverError && <Alert severity="error">{coverError}</Alert>}

              <Stack direction="row" spacing={2} alignItems="center">
                <Button variant="outlined" component="label">
                  Upload Audio File
                  <input hidden type="file" accept={AUDIO_ACCEPT} onChange={onFileChange('audioFile')} />
                </Button>
                <Typography variant="body2" color="text.secondary">
                  {formState.audioFile?.name ?? (isEditing ? 'Leave empty to keep current audio' : 'No file selected')}
                </Typography>
              </Stack>
              {audioError && <Alert severity="error">{audioError}</Alert>}
            </Stack>
          </DialogContent>
          <DialogActions>
            <Button onClick={closeFormDialog}>Cancel</Button>
            <Button type="submit" variant="contained">
              {isEditing ? 'Save Changes' : 'Upload'}
            </Button>
          </DialogActions>
        </Box>
      </Dialog>

      <Dialog open={isDeleteOpen} onClose={closeDeleteDialog}>
        <DialogTitle>Delete Audiobook</DialogTitle>
        <DialogContent>
          <Typography>
            Are you sure you want to delete {selectedAudiobook?.title ?? 'this audiobook'}?
          </Typography>
        </DialogContent>
        <DialogActions>
          <Button onClick={closeDeleteDialog}>Cancel</Button>
          <Button color="error" variant="contained" onClick={confirmDelete}>
            Delete
          </Button>
        </DialogActions>
      </Dialog>
    </Container>
  );
}
