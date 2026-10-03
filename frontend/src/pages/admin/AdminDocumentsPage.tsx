import { useState } from 'react';
import { Alert, Button, EmptyCard, Loading, PageHead, Pill, ReasonDialog } from '../../components';
import { useAction } from '../../hooks/useAction';
import { useLoad } from '../../hooks/useLoad';
import { api, openDocument } from '../../lib/api';
import { documentName } from '../../lib/documents';
import { date, dateTime } from '../../lib/format';
import { useToast } from '../../state/ToastContext';
import { StatusFilter, withStatus } from './StatusFilter';
import type { AdminDocument } from './types';

const FILTERS: [string, string][] = [
  ['Pending', 'Pending'],
  ['Verified', 'Verified'],
  ['Rejected', 'Rejected'],
  ['', 'All'],
];

export function AdminDocumentsPage() {
  const toast = useToast();
  const [status, setStatus] = useState('Pending');
  const documents = useLoad(() => api<AdminDocument[]>(withStatus('/admin/documents', status)), [status]);
  const [rejecting, setRejecting] = useState<AdminDocument | null>(null);
  const review = useAction();

  const decide = (id: number, approve: boolean, reason?: string) =>
    review.run(async () => {
      await api(`/admin/documents/${id}/review`, { body: { approve, reason } });
      setRejecting(null);
      toast(approve ? 'Document verified.' : 'Document rejected.');
      documents.reload();
    });

  const open = (id: number) => openDocument(id).catch((error) => toast(error.message, 'error'));

  return (
    <>
      <PageHead title="Documents" sub="KYC and vehicle documents. Check the expiry date against the file.">
        <StatusFilter options={FILTERS} value={status} onChange={setStatus} />
      </PageHead>

      {review.error && (
        <Alert kind="error" className="mb-sm">
          {review.error}
        </Alert>
      )}

      <Loading state={documents} />

      {documents.data?.length === 0 && <EmptyCard title="Nothing here" />}

      {!!documents.data?.length && (
        <div className="card table-wrap">
          <table>
            <thead>
              <tr>
                <th>Document</th>
                <th>Belongs to</th>
                <th>Number</th>
                <th>Valid until</th>
                <th>Uploaded</th>
                <th>Status</th>
                <th />
              </tr>
            </thead>
            <tbody>
              {documents.data.map((document) => (
                <tr key={document.id}>
                  <td>
                    <button type="button" className="btn btn-ghost btn-sm" onClick={() => open(document.id)}>
                      {documentName(document.docType)} ↗
                    </button>
                  </td>
                  <td className="small">
                    {document.entityType} #{document.entityId}
                  </td>
                  <td className="mono small">{document.documentNumber ?? '—'}</td>
                  <td>{date(document.expiryDate)}</td>
                  <td className="small">{dateTime(document.uploadedAt)}</td>
                  <td>
                    <Pill status={document.status} />
                  </td>
                  <td>
                    {document.status === 'Pending' && (
                      <div className="row tight">
                        <Button
                          size="sm"
                          variant="success"
                          busy={review.busy}
                          onClick={() => decide(document.id, true)}
                        >
                          Verify
                        </Button>
                        <Button size="sm" variant="danger" onClick={() => setRejecting(document)}>
                          Reject
                        </Button>
                      </div>
                    )}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      {rejecting && (
        <ReasonDialog
          title={`Reject ${documentName(rejecting.docType)}`}
          busy={review.busy}
          onClose={() => setRejecting(null)}
          onSubmit={(reason) => decide(rejecting.id, false, reason)}
        />
      )}
    </>
  );
}
