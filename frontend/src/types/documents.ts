import type { ListQuery } from './paging';

export type DocumentEntityType = 'Owner' | 'Driver' | 'Customer' | 'Vehicle';

/** GET /documents (admins see all; everyone else sees their own). */
export interface DocumentQuery extends ListQuery {
  status?: string;
}

export interface DocumentListItem {
  id: number;
  entityType: string;
  entityId: number;
  docType: string;
  fileName: string;
  documentNumber?: string | null;
  expiryDate?: string | null;
  status: string;
  rejectionReason?: string | null;
  uploadedAt: string;
}

/** POST /documents (multipart form). */
export interface UploadDocumentRequest {
  entityType: DocumentEntityType;
  /** Only for vehicles; KYC documents belong to the signed-in user. */
  entityId?: number;
  docType: string;
  documentNumber?: string;
  expiryDate?: string;
  file: File;
}
