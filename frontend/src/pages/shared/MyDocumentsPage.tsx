/** KYC documents for the signed-in owner or driver. */
import { useState } from 'react';
import { Alert, Button, EmptyCard, Loading, PageHead, Pill, UploadDialog } from '../../components';
import { useLoad } from '../../hooks/useLoad';
import { api, openDocument } from '../../lib/api';
import { OPTIONAL_KYC_DOCUMENTS, REQUIRED_KYC_DOCUMENTS, documentName } from '../../lib/documents';
import { date, dateTime } from '../../lib/format';
import { useAuth } from '../../state/AuthContext';
import { useToast } from '../../state/ToastContext';

interface MyDocument {
  id: number;
  docType: string;
  fileName: string;
  status: string;
  rejectionReason?: string;
  expiryDate?: string;
  uploadedAt: string;
}

interface Profile {
  detail?: { kycStatus?: string; rejectionReason?: string };
}

export function MyDocumentsPage() {
  const { user } = useAuth();
  const toast = useToast();
  const documents = useLoad(() => api<MyDocument[]>('/documents/mine'));
  const profile = useLoad(() => api<Profile>('/auth/me'));
  const [uploading, setUploading] = useState(false);

  const role = user!.role;
  const required = REQUIRED_KYC_DOCUMENTS[role] ?? [];
  const missing = required.filter(
    (type) =>
      !documents.data?.some((document) => document.docType === type && document.status !== 'Rejected'),
  );

  const handleUploaded = () => {
    setUploading(false);
    toast('Uploaded. We will review it shortly.');
    documents.reload();
  };

  return (
    <>
      <PageHead
        title={role === 'Owner' ? 'KYC documents' : 'My documents'}
        sub="Our team checks these before you can take trips. Files are stored privately."
      >
        <Button onClick={() => setUploading(true)}>Upload document</Button>
      </PageHead>

      <KycStatusAlert
        kycStatus={profile.data?.detail?.kycStatus}
        rejectionReason={profile.data?.detail?.rejectionReason}
        missing={missing}
      />

      <Loading state={documents} />

      {documents.data?.length === 0 && (
        <EmptyCard title="Nothing uploaded yet">
          Start with {required.map(documentName).join(', ')}.
        </EmptyCard>
      )}

      {!!documents.data?.length && (
        <div className="card table-wrap">
          <table>
            <thead>
              <tr>
                <th>Document</th>
                <th>File</th>
                <th>Valid until</th>
                <th>Uploaded</th>
                <th>Status</th>
              </tr>
            </thead>
            <tbody>
              {documents.data.map((document) => (
                <tr key={document.id}>
                  <td>
                    <b>{documentName(document.docType)}</b>
                  </td>
                  <td>
                    <button
                      type="button"
                      className="btn btn-ghost btn-sm"
                      onClick={() =>
                        openDocument(document.id).catch((error) => toast(error.message, 'error'))
                      }
                    >
                      {document.fileName}
                    </button>
                  </td>
                  <td>{date(document.expiryDate)}</td>
                  <td>{dateTime(document.uploadedAt)}</td>
                  <td>
                    <Pill status={document.status} />
                    {document.rejectionReason && (
                      <div className="small error-text mt-sm">{document.rejectionReason}</div>
                    )}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      {uploading && (
        <UploadDialog
          title="Upload a document"
          entityType={role}
          docTypes={[...required, ...(OPTIONAL_KYC_DOCUMENTS[role] ?? [])]}
          onClose={() => setUploading(false)}
          onDone={handleUploaded}
        />
      )}
    </>
  );
}

interface KycStatusAlertProps {
  kycStatus?: string;
  rejectionReason?: string;
  missing: string[];
}

function KycStatusAlert({ kycStatus, rejectionReason, missing }: KycStatusAlertProps) {
  switch (kycStatus) {
    case 'Approved':
      return (
        <Alert kind="success" title="You're verified" className="mb-md">
          You can take loads and trips.
        </Alert>
      );

    case 'Pending':
      return (
        <Alert kind="warn" title="Verification pending" className="mb-md">
          {missing.length > 0
            ? `Still needed: ${missing.map(documentName).join(', ')}.`
            : 'All documents received. We usually verify within a day.'}
        </Alert>
      );

    case 'Rejected':
      return (
        <Alert kind="error" title="Verification was not approved" className="mb-md">
          {rejectionReason} Upload corrected documents and we'll check again.
        </Alert>
      );

    default:
      return null;
  }
}
