import { openDocument } from '../lib/api';
import { documentName } from '../lib/documents';
import type { DocumentChip } from '../lib/types';
import { useToast } from '../state/ToastContext';

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
          onClick={() => openDocument(document.id).catch((error) => toast(error.message, 'error'))}
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
