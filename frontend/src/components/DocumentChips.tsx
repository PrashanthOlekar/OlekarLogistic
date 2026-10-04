import { documentsApi } from '../api/documentsApi';
import type { DocumentChip } from '../types';
import { documentName } from '../utils/documents';
import { useToast } from './Toast';

const CHIP_COLOUR: Record<string, string> = { Verified: 'green', Rejected: 'red' };

/** A row of document buttons, coloured by review status. Clicking one opens the file. */
export function DocumentChips({
  documents,
  showStatusMark,
}: {
  documents: DocumentChip[];
  showStatusMark?: boolean;
}) {
  const toast = useToast();

  if (documents.length === 0) {
    return <span className="small warn-text">No documents yet</span>;
  }

  return (
    <div className="row tight">
      {documents.map((document) => (
        <button
          key={document.id}
          type="button"
          className={`pill ${showStatusMark ? 'grey' : (CHIP_COLOUR[document.status] ?? 'grey')}`}
          onClick={() => documentsApi.open(document.id).catch((error) => toast(error.message, 'error'))}
        >
          {documentName(document.docType)}
          {showStatusMark && `: ${statusMark(document.status)}`}
        </button>
      ))}
    </div>
  );
}

function statusMark(status: string): string {
  if (status === 'Verified') return '✓';
  if (status === 'Rejected') return '✕';
  return '…';
}
