import { useState } from 'react';
import { documentsApi } from '../api/documentsApi';
import { useAction } from '../hooks/useAction';
import type { DocumentEntityType } from '../types';
import { DOCUMENTS_WITH_EXPIRY, documentName } from '../utils/documents';
import { Alert } from './Alert';
import { Button } from './Button';
import { Dialog } from './Dialog';
import { Field } from './Field';

interface UploadDialogProps {
  title: string;
  /** KYC documents belong to the signed-in owner or driver; vehicle documents need the vehicle id. */
  entityType: DocumentEntityType;
  /** Only needed for vehicles. */
  entityId?: number;
  docTypes: string[];
  onClose: () => void;
  onDone: () => void;
}

/** Upload form for KYC and vehicle documents (POST /documents). */
export function UploadDialog({ title, entityType, entityId, docTypes, onClose, onDone }: UploadDialogProps) {
  const [docType, setDocType] = useState(docTypes[0]);
  const [documentNumber, setDocumentNumber] = useState('');
  const [expiryDate, setExpiryDate] = useState('');
  const [file, setFile] = useState<File | null>(null);
  const upload = useAction();

  const isAadhaar = docType === 'Aadhaar';
  const asksForNumber = !isAadhaar && docType !== 'DriverPhoto';
  const needsExpiry = DOCUMENTS_WITH_EXPIRY.includes(docType);

  const submit = () =>
    upload.run(async () => {
      await documentsApi.upload({
        entityType,
        entityId,
        docType,
        documentNumber: isAadhaar ? undefined : documentNumber.trim() || undefined,
        expiryDate: expiryDate || undefined,
        file: file!,
      });
      onDone();
    });

  return (
    <Dialog title={title} onClose={onClose}>
      <Field label="Document">
        <select className="input" value={docType} onChange={(event) => setDocType(event.target.value)}>
          {docTypes.map((type) => (
            <option key={type} value={type}>
              {documentName(type)}
            </option>
          ))}
        </select>
      </Field>

      {asksForNumber && (
        <Field label="Document number (optional)">
          <input
            className="input"
            value={documentNumber}
            onChange={(event) => setDocumentNumber(event.target.value)}
          />
        </Field>
      )}

      {isAadhaar && (
        <Alert kind="info">
          Mask the first 8 digits of your Aadhaar before uploading. We only need the last 4.
        </Alert>
      )}

      {needsExpiry && (
        <Field label="Valid until">
          <input
            className="input"
            type="date"
            value={expiryDate}
            onChange={(event) => setExpiryDate(event.target.value)}
          />
        </Field>
      )}

      <Field label="File" hint="PDF or photo, up to 10 MB">
        <input
          className="input"
          type="file"
          accept=".pdf,image/*"
          onChange={(event) => setFile(event.target.files?.[0] ?? null)}
        />
      </Field>

      {upload.error && <Alert kind="error">{upload.error}</Alert>}

      <Button block busy={upload.busy} disabled={!file || (needsExpiry && !expiryDate)} onClick={submit}>
        Upload
      </Button>
    </Dialog>
  );
}
