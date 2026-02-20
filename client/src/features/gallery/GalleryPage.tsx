import { useCallback, useEffect, useMemo, useState } from 'react';
import {
  Alert,
  Box,
  Card,
  CardActions,
  CardContent,
  CardMedia,
  Chip,
  CircularProgress,
  Container,
  FormControl,
  Grid,
  IconButton,
  InputAdornment,
  InputLabel,
  MenuItem,
  Select,
  TextField,
  Tooltip,
  Typography,
} from '@mui/material';
import type { SelectChangeEvent } from '@mui/material';
import FavoriteBorderIcon from '@mui/icons-material/FavoriteBorder';
import FavoriteIcon from '@mui/icons-material/Favorite';
import PlayArrowIcon from '@mui/icons-material/PlayArrow';
import LibraryMusicIcon from '@mui/icons-material/LibraryMusic';
import SearchIcon from '@mui/icons-material/Search';
import ClearIcon from '@mui/icons-material/Clear';
import { useNavigate } from 'react-router-dom';
import { useAppDispatch } from '../../app/hooks';
import type { LibraryAudiobook } from '../../types';
import { libraryService } from '../../services/libraryService';
import { addFavoriteAsync, removeFavoriteAsync } from '../library/librarySlice';
import { GENRE_OPTIONS } from '../audiobooks/constants';

const resolveImageUrl = (path: string | null) => {
  if (!path) return null;
  if (path.startsWith('http://') || path.startsWith('https://')) return path;
  const baseUrl = import.meta.env.VITE_API_URL || 'https://localhost:7001';
  return `${baseUrl}${path}`;
};

const getGenreLabel = (value: string | null) =>
  GENRE_OPTIONS.find((g) => g.value === value)?.label ?? value ?? 'Other';

const SORT_OPTIONS = [
  { value: 'recent', label: 'Recently Added' },
  { value: 'title', label: 'Title A–Z' },
  { value: 'author', label: 'Author A–Z' },
] as const;

const DEBOUNCE_MS = 300;

export default function GalleryPage() {
  const navigate = useNavigate();
  const dispatch = useAppDispatch();

  const [books, setBooks] = useState<LibraryAudiobook[]>([]);
  const [authors, setAuthors] = useState<string[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const [searchQuery, setSearchQuery] = useState('');
  const [selectedGenre, setSelectedGenre] = useState('');
  const [selectedAuthor, setSelectedAuthor] = useState('');
  const [sortBy, setSortBy] = useState('recent');

  // Load authors once on mount
  useEffect(() => {
    libraryService.getDistinctAuthors().then(setAuthors).catch(() => {});
  }, []);

  // Fetch filtered books from backend whenever genre/author/sort changes
  const loadBooks = useCallback(async () => {
    setIsLoading(true);
    setError(null);
    try {
      const data = await libraryService.getGalleryFiltered({
        genre: selectedGenre || undefined,
        author: selectedAuthor || undefined,
        sortBy: sortBy || undefined,
      });
      setBooks(data);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to load gallery');
    } finally {
      setIsLoading(false);
    }
  }, [selectedGenre, selectedAuthor, sortBy]);

  useEffect(() => {
    void loadBooks();
  }, [loadBooks]);

  // Client-side search filter with debounce
  const [debouncedSearch, setDebouncedSearch] = useState('');
  useEffect(() => {
    const timer = setTimeout(() => setDebouncedSearch(searchQuery), DEBOUNCE_MS);
    return () => clearTimeout(timer);
  }, [searchQuery]);

  const filteredBooks = useMemo(() => {
    const q = debouncedSearch.trim().toLowerCase();
    if (!q) return books;
    return books.filter(
      (b) =>
        b.title.toLowerCase().includes(q) ||
        b.author.toLowerCase().includes(q) ||
        (b.genre && getGenreLabel(b.genre).toLowerCase().includes(q)),
    );
  }, [books, debouncedSearch]);

  const hasActiveFilters = selectedGenre || selectedAuthor || debouncedSearch;

  const clearAllFilters = () => {
    setSearchQuery('');
    setSelectedGenre('');
    setSelectedAuthor('');
    setSortBy('recent');
  };

  const onFavoriteToggle = async (book: LibraryAudiobook) => {
    if (book.isFavorite) {
      await dispatch(removeFavoriteAsync(book.id));
    } else {
      await dispatch(addFavoriteAsync(book.id));
    }
    setBooks((prev) =>
      prev.map((b) => (b.id === book.id ? { ...b, isFavorite: !b.isFavorite } : b)),
    );
  };

  const handleGenreChange = (e: SelectChangeEvent) => setSelectedGenre(e.target.value);
  const handleAuthorChange = (e: SelectChangeEvent) => setSelectedAuthor(e.target.value);
  const handleSortChange = (e: SelectChangeEvent) => setSortBy(e.target.value);

  return (
    <Container maxWidth="lg" sx={{ py: 4 }}>
      {/* Header */}
      <Typography variant="h4" sx={{ fontWeight: 700, color: 'primary.main', mb: 0.5 }}>
        Audiobook Gallery
      </Typography>
      <Typography variant="body1" color="text.secondary" sx={{ mb: 3 }}>
        Browse, search, and filter the full audiobook catalog.
      </Typography>

      {error && (
        <Alert severity="error" sx={{ mb: 2 }}>
          {error}
        </Alert>
      )}

      {/* Search & Filter Bar */}
      <Box
        sx={{
          display: 'flex',
          flexWrap: 'wrap',
          gap: 2,
          mb: 3,
          alignItems: 'center',
        }}
      >
        {/* Search */}
        <TextField
          size="small"
          placeholder="Search by title, author, or genre…"
          value={searchQuery}
          onChange={(e) => setSearchQuery(e.target.value)}
          slotProps={{
            input: {
              startAdornment: (
                <InputAdornment position="start">
                  <SearchIcon fontSize="small" />
                </InputAdornment>
              ),
              endAdornment: searchQuery ? (
                <InputAdornment position="end">
                  <IconButton size="small" onClick={() => setSearchQuery('')}>
                    <ClearIcon fontSize="small" />
                  </IconButton>
                </InputAdornment>
              ) : null,
            },
          }}
          sx={{ minWidth: 260, flex: 1 }}
        />

        {/* Genre Filter */}
        <FormControl size="small" sx={{ minWidth: 160 }}>
          <InputLabel>Genre</InputLabel>
          <Select value={selectedGenre} label="Genre" onChange={handleGenreChange}>
            <MenuItem value="">All Genres</MenuItem>
            {GENRE_OPTIONS.map((g) => (
              <MenuItem key={g.value} value={g.value}>
                {g.label}
              </MenuItem>
            ))}
          </Select>
        </FormControl>

        {/* Author Filter */}
        <FormControl size="small" sx={{ minWidth: 160 }}>
          <InputLabel>Author</InputLabel>
          <Select value={selectedAuthor} label="Author" onChange={handleAuthorChange}>
            <MenuItem value="">All Authors</MenuItem>
            {authors.map((a) => (
              <MenuItem key={a} value={a}>
                {a}
              </MenuItem>
            ))}
          </Select>
        </FormControl>

        {/* Sort */}
        <FormControl size="small" sx={{ minWidth: 160 }}>
          <InputLabel>Sort By</InputLabel>
          <Select value={sortBy} label="Sort By" onChange={handleSortChange}>
            {SORT_OPTIONS.map((s) => (
              <MenuItem key={s.value} value={s.value}>
                {s.label}
              </MenuItem>
            ))}
          </Select>
        </FormControl>
      </Box>

      {/* Active filter chips */}
      {hasActiveFilters && (
        <Box sx={{ display: 'flex', gap: 1, flexWrap: 'wrap', mb: 2 }}>
          {selectedGenre && (
            <Chip
              label={`Genre: ${getGenreLabel(selectedGenre)}`}
              onDelete={() => setSelectedGenre('')}
              size="small"
              color="primary"
              variant="outlined"
            />
          )}
          {selectedAuthor && (
            <Chip
              label={`Author: ${selectedAuthor}`}
              onDelete={() => setSelectedAuthor('')}
              size="small"
              color="primary"
              variant="outlined"
            />
          )}
          {debouncedSearch && (
            <Chip
              label={`Search: "${debouncedSearch}"`}
              onDelete={() => setSearchQuery('')}
              size="small"
              color="primary"
              variant="outlined"
            />
          )}
          <Chip label="Clear All" onClick={clearAllFilters} size="small" color="primary" />
        </Box>
      )}

      {/* Loading */}
      {isLoading ? (
        <Box sx={{ display: 'flex', justifyContent: 'center', py: 8 }}>
          <CircularProgress />
        </Box>
      ) : filteredBooks.length === 0 ? (
        /* No results */
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
          <Typography variant="h6">
            {hasActiveFilters ? 'No audiobooks match your filters' : 'No audiobooks available yet'}
          </Typography>
          {hasActiveFilters && (
            <Typography
              variant="body2"
              sx={{ cursor: 'pointer', textDecoration: 'underline' }}
              onClick={clearAllFilters}
            >
              Clear all filters
            </Typography>
          )}
        </Box>
      ) : (
        /* Results Grid */
        <>
          <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
            Showing {filteredBooks.length} audiobook{filteredBooks.length !== 1 ? 's' : ''}
          </Typography>

          <Grid container spacing={2}>
            {filteredBooks.map((book) => (
              <Grid key={book.id} size={{ xs: 12, sm: 6, md: 4, lg: 3 }}>
                <Card
                  sx={{
                    height: '100%',
                    display: 'flex',
                    flexDirection: 'column',
                    transition: 'transform 0.2s, box-shadow 0.2s',
                    '&:hover': {
                      transform: 'translateY(-4px)',
                      boxShadow: 6,
                    },
                  }}
                >
                  {resolveImageUrl(book.coverImageUrl) ? (
                    <CardMedia
                      component="img"
                      height="220"
                      image={resolveImageUrl(book.coverImageUrl)!}
                      alt={`${book.title} cover`}
                      sx={{ objectFit: 'cover' }}
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
                      <LibraryMusicIcon sx={{ fontSize: 64 }} color="action" />
                    </Box>
                  )}
                  <CardContent sx={{ flexGrow: 1, pb: 1 }}>
                    <Typography variant="subtitle1" fontWeight={600} noWrap title={book.title}>
                      {book.title}
                    </Typography>
                    <Typography variant="body2" color="text.secondary" noWrap title={book.author}>
                      {book.author}
                    </Typography>
                    {book.genre && (
                      <Chip
                        label={getGenreLabel(book.genre)}
                        size="small"
                        sx={{ mt: 0.5, fontSize: '0.7rem' }}
                      />
                    )}
                    {book.description && (
                      <Typography
                        variant="body2"
                        color="text.secondary"
                        sx={{
                          mt: 1,
                          display: '-webkit-box',
                          WebkitLineClamp: 2,
                          WebkitBoxOrient: 'vertical',
                          overflow: 'hidden',
                        }}
                        title={book.description}
                      >
                        {book.description}
                      </Typography>
                    )}
                  </CardContent>
                  <CardActions>
                    <Tooltip title="Play audiobook">
                      <IconButton
                        color="primary"
                        onClick={() => navigate(`/player/${book.id}`, { state: { from: '/' } })}
                      >
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
        </>
      )}
    </Container>
  );
}
