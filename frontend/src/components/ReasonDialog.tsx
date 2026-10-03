import { useState } from 'react';
import { Button } from './Button';
import { Dialog } from './Dialog';
import { Field } from './Field';

interface ReasonDialogProps {
  title: string;
  busy?: boolean;
  onSubmit: (reason: string) => void;
  onClose: () => void;
}

/** Asks for a reason before rejecting something (the API requires one). */
export function ReasonDialog({ title, busy, onSubmit, onClose }: ReasonDialogProps) {
  const [reason, setReason] = useState('');

  return (
    <Dialog title={title} onClose={onClose}>
      <Field
        label="Reason shown to the applicant"
        hint="Say what to fix, for example “Insurance copy is unreadable”."
      >
        <textarea
          className="input"
          value={reason}
          onChange={(event) => setReason(event.target.value)}
          autoFocus
        />
      </Field>
      <div className="row end">
        <Button variant="secondary" onClick={onClose}>
          Cancel
        </Button>
        <Button
          variant="danger"
          busy={busy}
          disabled={!reason.trim()}
          onClick={() => onSubmit(reason.trim())}
        >
          Reject
        </Button>
      </div>
    </Dialog>
  );
}
