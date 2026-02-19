export const GENRE_OPTIONS = [
  { value: 'Fiction', label: 'Fiction' },
  { value: 'NonFiction', label: 'Non-Fiction' },
  { value: 'Mystery', label: 'Mystery' },
  { value: 'Thriller', label: 'Thriller' },
  { value: 'Romance', label: 'Romance' },
  { value: 'Fantasy', label: 'Fantasy' },
  { value: 'ScienceFiction', label: 'Science Fiction' },
  { value: 'Biography', label: 'Biography' },
  { value: 'SelfHelp', label: 'Self Help' },
  { value: 'History', label: 'History' },
  { value: 'Business', label: 'Business' },
  { value: 'Children', label: 'Children' },
  { value: 'YoungAdult', label: 'Young Adult' },
  { value: 'Horror', label: 'Horror' },
  { value: 'Poetry', label: 'Poetry' },
] as const;

export const AUDIO_FORMATS = ['.mp3', '.mp4', '.m4a', '.aac', '.wav', '.ogg', '.flac', '.webm'] as const;
export const IMAGE_FORMATS = ['.jpg', '.jpeg', '.png', '.webp', '.gif', '.bmp'] as const;

export const AUDIO_ACCEPT = AUDIO_FORMATS.join(',');
export const IMAGE_ACCEPT = IMAGE_FORMATS.join(',');
