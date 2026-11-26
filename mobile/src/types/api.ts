/**
 * Backend DTO'larının TypeScript karşılıkları.
 * Kaynak: src/SesliAsistan.Business/DTOs (ASP.NET varsayılanı camelCase JSON).
 * Backend'de bir alan değişirse burası da güncellenmeli.
 */

// --- Auth (FG-1) ---
export type UserDto = {
  id: string;
  email: string;
  fullName: string;
  role: 'Student' | 'Admin' | string;
  preferredVoice: string;
  playbackSpeed: number;
  createdAt: string;
};

export type AuthResponse = {
  accessToken: string;
  accessTokenExpiresAt: string;
  refreshToken: string;
  user: UserDto;
};

export type RegisterRequest = { email: string; password: string; fullName: string };
export type LoginRequest = { email: string; password: string };
export type UpdateProfileRequest = { fullName: string; preferredVoice: string; playbackSpeed: number };
export type ChangePasswordRequest = { currentPassword: string; newPassword: string };
export type ForgotPasswordRequest = { email: string };
export type ResetPasswordRequest = { email: string; code: string; newPassword: string };

// --- Belgeler (FG-2, FG-5) ---
export type DocumentStatus = 'Uploaded' | 'Processing' | 'Ready' | 'Failed';

export type DocumentListItemDto = {
  id: string;
  title: string;
  fileType: string;
  fileSizeBytes: number;
  status: DocumentStatus;
  createdAt: string;
};

export type SectionNodeDto = {
  id: string;
  title: string;
  level: number;
  orderIndex: number;
  children: SectionNodeDto[];
};

export type DocumentDetailDto = {
  id: string;
  title: string;
  originalFileName: string;
  fileType: string;
  fileSizeBytes: number;
  status: DocumentStatus;
  errorMessage: string | null;
  pageCount: number | null;
  characterCount: number | null;
  createdAt: string;
  processedAt: string | null;
  sections: SectionNodeDto[];
};

export type SectionContentDto = { id: string; title: string; level: number; content: string };

export type PagedResult<T> = { items: T[]; page: number; pageSize: number; totalCount: number; totalPages: number };
