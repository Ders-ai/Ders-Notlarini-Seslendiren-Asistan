namespace SesliAsistan.Entities.Enums;

public enum UserRole { Student = 1, Admin = 2 }

public enum DocumentFileType { Pdf = 1, Docx = 2 }

/// <summary>Belge işleme hattının durumları (asenkron metin çıkarma).</summary>
public enum DocumentStatus { Uploaded = 0, Processing = 1, Ready = 2, Failed = 3 }

/// <summary>FG-7: Paylaşım gizlilik düzeyi.</summary>
public enum PrivacyLevel { Private = 0, Friends = 1, Group = 2, Public = 3 }

public enum FriendshipStatus { Pending = 0, Accepted = 1, Rejected = 2, Blocked = 3 }

/// <summary>FG-4: Özet uzunluğu.</summary>
public enum SummaryLength { Short = 1, Medium = 2, Detailed = 3 }

public enum SharedContentType { Summary = 1, FlashcardDeck = 2, Quiz = 3 }

public enum ReportStatus { Open = 0, UnderReview = 1, Reviewed = 2, ContentRemoved = 3, Dismissed = 4 }

public enum GroupRole { Member = 0, Owner = 1 }

/// <summary>FG-6: GenAI test üretim durumu.</summary>
public enum QuizStatus { Pending = 0, Ready = 1, Failed = 2 }

/// <summary>FG-8: Yönetici moderasyon işlem türü.</summary>
public enum AdminActionType { RemoveContent = 1, DismissReport = 2, SuspendUser = 3, UnsuspendUser = 4 }

