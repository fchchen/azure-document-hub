export interface Document {
  id: string;
  fileName: string;
  contentType: string;
  fileSize: number;
  status: DocumentStatus;
  thumbnailUrl?: string;
  metadata?: DocumentMetadata;
  uploadedBy: string;
  createdAt: Date;
  processedAt?: Date;
}

export interface DocumentMetadata {
  pageCount?: number;
  author?: string;
  title?: string;
  customProperties?: { [key: string]: string };
}

export interface DocumentUploadResponse {
  id: string;
  fileName: string;
  status: string;
  createdAt: Date;
}

export interface DocumentListResponse {
  documents: Document[];
  totalCount: number;
  page: number;
  pageSize: number;
}

export type DocumentStatus = 'Pending' | 'Processing' | 'Completed' | 'Failed';
