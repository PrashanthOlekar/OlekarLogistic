import { useState } from 'react';
import { api, openDocument } from '../api';
import { useAuth, useToast } from '../state';
import { Alert, Button, Empty, Loading, PageHead, Pill, date, dateTime, useLoad } from '../components/ui';
import { UploadDialog, docName } from './owner/OwnerPages';

interface Doc { id: number; docType: string; fileName: string; status: string; rejectionReason?: string; expiryDate?: string; uploadedAt: string }

const REQUIRED: Record<string, string[]> = {
  Owner: ['Aadhaar', 'PAN', 'CancelledCheque'],
  Driver: ['Licence', 'Aadhaar', 'DriverPhoto']
};
const OPTIONAL: Record<string, string[]> = { Owner: ['GST', 'Other'], Driver: ['Other'] };

/** KYC documents for the signed-in owner or driver. */
export function MyDocumentsPage() {
  const { user } = useAuth();
  const toast = useToast();
  const role = user!.role;
  const docs = useLoad(() => api<Doc[]>('/documents/mine'));
  const me = useLoad(() => api<{ detail?: { kycStatus?: string; rejectionReason?: string } }>('/auth/me'));
  const [uploading, setUploading] = useState(false);
  const required = REQUIRED[role] ?? [];
  const missing = required.filter((t) => !docs.data?.some((d) => d.docType === t && d.status !== 'Rejected'));
  const kyc = me.data?.detail?.kycStatus;

  return (
    <>
      <PageHead title={role === 'Owner' ? 'KYC documents' : 'My documents'} sub="Our team checks these before you can take trips. Files are stored privately.">
        <Button onClick={() => setUploading(true)}>Upload document</Button>
      </PageHead>
      {kyc && (
        <div style={{ marginBottom: 16 }}>
          {kyc === 'Approved' && <Alert kind="success" title="You're verified">You can take loads and trips.</Alert>}
          {kyc === 'Pending' && <Alert kind="warn" title="Verification pending">{missing.length ? <>Still needed: {missing.map(docName).join(', ')}.</> : 'All documents received. We usually verify within a day.'}</Alert>}
          {kyc === 'Rejected' && <Alert kind="error" title="Verification was not approved">{me.data?.detail?.rejectionReason} Upload corrected documents and we'll check again.</Alert>}
        </div>
      )}
      <Loading state={docs} />
      {docs.data?.length === 0 && <div className="card"><Empty title="Nothing uploaded yet">Start with {required.map(docName).join(', ')}.</Empty></div>}
      {!!docs.data?.length && (
        <div className="card table-wrap">
          <table>
            <thead><tr><th>Document</th><th>File</th><th>Valid until</th><th>Uploaded</th><th>Status</th></tr></thead>
            <tbody>{docs.data.map((d) => (
              <tr key={d.id}>
                <td><b>{docName(d.docType)}</b></td>
                <td><button type="button" className="btn btn-ghost btn-sm" onClick={() => openDocument(d.id).catch((e) => toast(e.message, 'error'))}>{d.fileName}</button></td>
                <td>{date(d.expiryDate)}</td>
                <td>{dateTime(d.uploadedAt)}</td>
                <td><Pill status={d.status} />{d.rejectionReason && <div className="small" style={{ color: 'var(--red)', marginTop: 4 }}>{d.rejectionReason}</div>}</td>
              </tr>
            ))}</tbody>
          </table>
        </div>
      )}
      {uploading && (
        <UploadDialog title="Upload a document" entityType={role} docTypes={[...required, ...(OPTIONAL[role] ?? [])]}
          onClose={() => setUploading(false)} onDone={() => { setUploading(false); toast('Uploaded. We will review it shortly.'); docs.reload(); }} />
      )}
    </>
  );
}
