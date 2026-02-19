import { useEffect, useMemo, useState } from 'react';
import {
  Alert,
  Box,
  Card,
  CardActions,
  CardContent,
  CardMedia,
  CircularProgress,
  Container,
  Grid,
  IconButton,
  Tooltip,
  Typography,
} from '@mui/material';
import FavoriteBorderIcon from '@mui/icons-material/FavoriteBorder';
import FavoriteIcon from '@mui/icons-material/Favorite';
import PlayArrowIcon from '@mui/icons-material/PlayArrow';
import LibraryMusicIcon from '@mui/icons-material/LibraryMusic';
import { useNavigate } from 'react-router-dom';
import { useAppDispatch } from '../../app/hooks';
import type { LibraryAudiobook } from '../../types';
import { libraryService } from '../../services/libraryService';
import { addFavoriteAsync, removeFavoriteAsync } from '../library/librarySlice';
import { GENRE_OPTIONS } from '../audiobooks/constants';

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
  GENRE_OPTIONS.find((genre) => genre.value === value)?.label ?? value ?? 'Other';

export default function GalleryPage() {
  const navigate = useNavigate();
  const dispatch = useAppDispatch();
  const [books, setBooks] = useState<LibraryAudiobook[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const loadGallery = async () => {
    setIsLoading(true);
    setError(null);
    try {
      const data = await libraryService.getGalleryBooks();
      setBooks(data);
    } catch (loadError) {
      const message = loadError instanceof Error ? loadError.message : 'Failed to load gallery';
      setError(message);
    } finally {
      setIsLoading(false);
    }
  };

  useEffect(() => {
    void loadGallery();
  }, []);

  const booksByGenre = useMemo(() => {
    return books.reduce<Record<string, LibraryAudiobook[]>>((acc, book) => {
      const key = getGenreLabel(book.genre);
      if (!acc[key]) {
        acc[key] = [];
      }
      acc[key].push(book);
      return acc;
    }, {});
  }, [books]);

  const genres = useMemo(() => Object.keys(booksByGenre).sort((a, b) => a.localeCompare(b)), [booksByGenre]);

  const onFavoriteToggle = async (book: LibraryAudiobook) => {
    if (book.isFavorite) {
      await dispatch(removeFavoriteAsync(book.id));
    } else {
      await dispatch(addFavoriteAsync(book.id));
    }

    await loadGallery();
  };

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
      <Typography variant="h4" sx={{ fontWeight: 700, color: 'primary.main', mb: 1 }}>
        Audiobook Gallery
      </Typography>
      <Typography variant="body1" color="text.secondary" sx={{ mb: 3 }}>
        Discover all uploaded audiobooks, grouped by genre.
      </Typography>

      {error && (
        <Alert severity="error" sx={{ mb: 2 }}>
          {error}
        </Alert>
      )}

      {genres.length === 0 ? (
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
          <Typography variant="h6">No audiobooks available yet</Typography>
        </Box>
      ) : (
        genres.map((genre) => (
          <Box key={genre} sx={{ mt: 4 }}>
            <Typography variant="h5" sx={{ fontWeight: 700, mb: 2 }}>
              {genre}
            </Typography>
            <Grid container spacing={2}>
              {booksByGenre[genre].map((book) => (
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
                    </CardContent>
                    <CardActions>
                      <Tooltip title="Play audiobook">
                        <IconButton color="primary" onClick={() => navigate(`/player/${book.id}`)}>
                          <PlayArrowIcon />
                        </IconButton>
                      </Tooltip>
                      <Tooltip title={book.isFavorite ? 'Remove from favourites' : 'Add to favourites'}>
                        <IconButton color="primary" onClick={() => void onFavoriteToggle(book)}>
                          {book.isFavorite ? <FavoriteIcon /> : <FavoriteBorderIcon />}
                        </IconButton>
                      </Tooltip>
                    </CardActions>
                  </Card>
                </Grid>
              ))}
            </Grid>
          </Box>
        ))
      )}
    </Container>
  );
}
