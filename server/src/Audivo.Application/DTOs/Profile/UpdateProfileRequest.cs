using Audivo.Application.DTOs.Audiobooks;

namespace Audivo.Application.DTOs.Profile;

public sealed record UpdateProfileRequest(
    string FirstName,
    string LastName,
    FileUpload? ProfileImage);
