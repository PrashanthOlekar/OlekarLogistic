import { useState } from 'react';
import { documentsApi } from '../../api/documentsApi';
import {
  Alert,
  Button,
  EmptyCard,
  Loading,
  PageHead,
  Pagination,
  Pill,
  ReasonDialog,
  useToast,
} from '../../components';
import { StatusFilter } from '../../features/admin/StatusFilter';
import { useAction } from '../../hooks/useAction';
import { usePagedLoad } from '../../hooks/usePagedLoad';
import type { DocumentListItem } from '../../types';
import { documentName } from '../../utils/documents';
import { date, dateTime } from '../../utils/format';

const FILTERS: [string, string][] = [
  ['Pending', 'Pending'],
  ['Verified', 'Verified'],
  ['Rejected', 'Rejected'],
  ['', 'All'],
];

export function AdminDocumentsPage() {
  const toast = useToast();
  const [status, setStatus] = useState('Pending');
  const documents = usePagedLoad((page) => documentsApi.list({ ...page, status }), [status]);
  const [rejecting, setRejecting] = useState<DocumentListItem | null>(null);
  const review = useAction();

  const decide = (id: number, approve: boolean, reason?: string) =>
    review.run(async () => {
      await documentsApi.review(id, { approve, reason });
      setRejecting(null);
      toast(approve ? 'Document verified.' : 'Document rejected.');
      documents.reload();
    });

  const open = (id: number) => documentsApi.open(id).catch((error) => toast(error.message, 'error'));

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

      {documents.items?.length === 0 && <EmptyCard title="Nothing here" />}

      {!!documents.items?.length && (
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
              {documents.items.map((document) => (
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
          <Pagination page={documents.data} onPageChange={documents.setPageNumber} />
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
