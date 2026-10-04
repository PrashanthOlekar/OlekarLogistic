/** /documents: KYC and vehicle documents. Files are private, so they are downloaded with the token. */
import type {
  CreatedResource,
  DocumentListItem,
  DocumentQuery,
  PagedResult,
  UploadDocumentRequest,
  VerificationRequest,
} from '../types';
import { apiClient, cleanParams } from './apiClient';
import { toApiErrorFromBlob } from './errors';

/** How long the downloaded copy stays openable in the new tab. */
const OBJECT_URL_LIFETIME_MS = 60_000;

export const documentsApi = {
  async list(query: DocumentQuery = {}): Promise<PagedResult<DocumentListItem>> {
    const { data } = await apiClient.get<PagedResult<DocumentListItem>>('/documents', { params: cleanParams(query) });
    return data;
  },

  async upload(request: UploadDocumentRequest): Promise<CreatedResource> {
    const form = new FormData();
    form.set('entityType', request.entityType);
    if (request.entityId) {
      form.set('entityId', String(request.entityId));
    }
    form.set('docType', request.docType);
    if (request.documentNumber) {
      form.set('documentNumber', request.documentNumber);
    }
    if (request.expiryDate) {
      form.set('expiryDate', request.expiryDate);
    }
    form.set('file', request.file);

    const { data } = await apiClient.post<CreatedResource>('/documents', form);
    return data;
  },

  async review(id: number, request: VerificationRequest): Promise<void> {
    await apiClient.put(`/documents/${id}/review`, request);
  },

  /** Downloads a document and opens it in a new tab. */
  async open(id: number): Promise<void> {
    // Open the tab first, while the click still counts as a user action, so pop-up blockers allow it.
    const tab = window.open('', '_blank');
    try {
      const { data } = await apiClient.get<Blob>(`/documents/${id}/file`, { responseType: 'blob' });
      const url = URL.createObjectURL(data);
      if (tab) {
        tab.opener = null;
        tab.location.href = url;
      } else {
        window.open(url, '_blank', 'noopener');
      }
      setTimeout(() => URL.revokeObjectURL(url), OBJECT_URL_LIFETIME_MS);
    } catch (error) {
      tab?.close();
      throw await toApiErrorFromBlob(error);
    }
  },
};
