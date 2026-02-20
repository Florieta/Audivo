import { useEffect, useMemo, useState, type ChangeEvent } from 'react';
import {
  Alert,
  Avatar,
  Box,
  Button,
  Card,
  CardContent,
  CircularProgress,
  Container,
  Stack,
  TextField,
  Typography,
} from '@mui/material';
import { useAppDispatch, useAppSelector } from '../../app/hooks';
import { setCredentials } from '../auth/authSlice';
import { profileService } from '../../services/profileService';
import type { ProfileResponse } from '../../types';

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

export default function ProfilePage() {
  const dispatch = useAppDispatch();
  const { accessToken } = useAppSelector((state) => state.auth);

  const [profile, setProfile] = useState<ProfileResponse | null>(null);
  const [firstName, setFirstName] = useState('');
  const [lastName, setLastName] = useState('');
  const [selectedImage, setSelectedImage] = useState<File | null>(null);
  const [previewUrl, setPreviewUrl] = useState<string | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [isSaving, setIsSaving] = useState(false);
  const [isEditing, setIsEditing] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const load = async () => {
      setIsLoading(true);
      setError(null);
      try {
        const data = await profileService.getCurrentProfile();
        setProfile(data);
        setFirstName(data.firstName);
        setLastName(data.lastName);
      } catch (loadError) {
        setError(loadError instanceof Error ? loadError.message : 'Failed to load profile.');
      } finally {
        setIsLoading(false);
      }
    };

    void load();
  }, []);

  useEffect(() => {
    if (!selectedImage) {
      setPreviewUrl(null);
      return;
    }

    const localUrl = URL.createObjectURL(selectedImage);
    setPreviewUrl(localUrl);
    return () => URL.revokeObjectURL(localUrl);
  }, [selectedImage]);

  const initials = useMemo(() => {
    const first = (firstName || profile?.firstName || '').trim().charAt(0);
    const last = (lastName || profile?.lastName || '').trim().charAt(0);
    return `${first}${last}`.toUpperCase() || 'U';
  }, [firstName, lastName, profile?.firstName, profile?.lastName]);

  const displayImage = previewUrl ?? resolveImageUrl(profile?.profileImageUrl ?? null);

  const onImageChange = (event: ChangeEvent<HTMLInputElement>) => {
    const file = event.target.files?.[0] ?? null;
    if (!file) {
      setSelectedImage(null);
      return;
    }

    const allowed = ['image/jpeg', 'image/png'];
    if (!allowed.includes(file.type)) {
      setError('Only JPG and PNG images are supported.');
      return;
    }

    setError(null);
    setSelectedImage(file);
  };

  const onSave = async () => {
    if (!profile) {
      return;
    }

    setIsSaving(true);
    setError(null);
    try {
      const updated = await profileService.updateCurrentProfile({
        firstName: firstName.trim(),
        lastName: lastName.trim(),
        profileImage: selectedImage,
      });

      setProfile(updated);
      setFirstName(updated.firstName);
      setLastName(updated.lastName);
      setSelectedImage(null);
      setIsEditing(false);

      if (accessToken) {
        dispatch(
          setCredentials({
            accessToken,
            user: {
              email: updated.email,
              firstName: updated.firstName,
              lastName: updated.lastName,
              profileImageUrl: updated.profileImageUrl,
            },
          }),
        );
      }
    } catch (saveError) {
      setError(saveError instanceof Error ? saveError.message : 'Failed to update profile.');
    } finally {
      setIsSaving(false);
    }
  };

  if (isLoading) {
    return (
      <Container maxWidth="sm" sx={{ py: 6 }}>
        <Box sx={{ display: 'flex', justifyContent: 'center' }}>
          <CircularProgress />
        </Box>
      </Container>
    );
  }

  return (
    <Container maxWidth="md" sx={{ py: 4 }}>
      <Card sx={{ maxWidth: 620, mx: 'auto' }}>
        <CardContent>
          <Stack spacing={2.5} alignItems="center">
            {displayImage ? (
              <Avatar src={displayImage} alt={profile?.fullName} sx={{ width: 120, height: 120 }} />
            ) : (
              <Avatar sx={{ width: 120, height: 120, fontSize: 40, bgcolor: 'primary.main' }}>{initials}</Avatar>
            )}

            <Typography variant="h5" sx={{ fontWeight: 700 }}>
              {profile?.fullName || `${firstName} ${lastName}`.trim()}
            </Typography>

            <Typography variant="body2" color="text.secondary">
              {profile?.email}
            </Typography>

            <Typography variant="caption" color="text.secondary">
              Account created: {profile?.createdAt ? new Date(profile.createdAt).toLocaleDateString() : '-'}
            </Typography>

            {error && (
              <Alert severity="error" sx={{ width: '100%' }}>
                {error}
              </Alert>
            )}

            {isEditing && (
              <Stack spacing={2} sx={{ width: '100%' }}>
                <TextField
                  label="First Name"
                  value={firstName}
                  onChange={(event) => setFirstName(event.target.value)}
                  fullWidth
                />
                <TextField
                  label="Last Name"
                  value={lastName}
                  onChange={(event) => setLastName(event.target.value)}
                  fullWidth
                />

                <Button variant="outlined" component="label">
                  Upload Profile Photo
                  <input hidden type="file" accept="image/jpeg,image/png" onChange={onImageChange} />
                </Button>
                <Typography variant="caption" color="text.secondary">
                  {selectedImage?.name ?? 'JPG or PNG up to 5MB'}
                </Typography>
              </Stack>
            )}

            {!isEditing ? (
              <Button variant="contained" onClick={() => setIsEditing(true)}>
                Edit Profile
              </Button>
            ) : (
              <Stack direction="row" spacing={1.5}>
                <Button
                  variant="outlined"
                  onClick={() => {
                    setIsEditing(false);
                    setSelectedImage(null);
                    setFirstName(profile?.firstName ?? '');
                    setLastName(profile?.lastName ?? '');
                    setError(null);
                  }}
                >
                  Cancel
                </Button>
                <Button variant="contained" onClick={() => void onSave()} disabled={isSaving}>
                  {isSaving ? 'Saving...' : 'Save Changes'}
                </Button>
              </Stack>
            )}
          </Stack>
        </CardContent>
      </Card>
    </Container>
  );
}
