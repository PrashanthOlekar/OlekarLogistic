import { useState } from 'react';
import { Alert, Button, Dialog, Field } from '../../components';
import { useAction } from '../../hooks/useAction';
import { api } from '../../lib/api';
import { inr } from '../../lib/format';

const PAYMENT_METHODS = [
  { value: 'UPI', label: 'UPI (GPay, PhonePe, Paytm)' },
  { value: 'CreditCard', label: 'Credit card' },
  { value: 'DebitCard', label: 'Debit card' },
  { value: 'NetBanking', label: 'Net banking' },
  { value: 'Wallet', label: 'Wallet' },
];

interface PayDialogProps {
  bookingId: string;
  amount: number;
  onClose: () => void;
  onPaid: () => void;
}

/** In test mode no money moves; with gateway keys this becomes the real checkout. */
export function PayDialog({ bookingId, amount, onClose, onPaid }: PayDialogProps) {
  const [method, setMethod] = useState('UPI');
  const pay = useAction();

  const submit = () =>
    pay.run(async () => {
      await api(`/bookings/${bookingId}/pay`, { body: { method } });
      onPaid();
    });

  return (
    <Dialog title={`Pay ${inr(amount)}`} onClose={onClose}>
      <Field label="Payment method">
        <select className="input" value={method} onChange={(event) => setMethod(event.target.value)}>
          {PAYMENT_METHODS.map((option) => (
            <option key={option.value} value={option.value}>
              {option.label}
            </option>
          ))}
        </select>
      </Field>
      <Alert kind="info">
        Test mode: no money moves. Once your payment gateway keys are added, this opens the real checkout.
      </Alert>
      {pay.error && <Alert kind="error">{pay.error}</Alert>}
      <Button size="lg" block busy={pay.busy} onClick={submit}>
        Pay securely
      </Button>
    </Dialog>
  );
}

interface CancelBookingDialogProps {
  bookingId: string;
  isPaid: boolean;
  onClose: () => void;
  onCancelled: () => void;
}

export function CancelBookingDialog({ bookingId, isPaid, onClose, onCancelled }: CancelBookingDialogProps) {
  const [reason, setReason] = useState('');
  const cancel = useAction();

  const submit = () =>
    cancel.run(async () => {
      await api(`/bookings/${bookingId}/cancel`, { body: { reason } });
      onCancelled();
    });

  return (
    <Dialog title="Cancel this booking?" onClose={onClose}>
      <p className="muted">
        {isPaid ? 'Your payment will be refunded in full.' : 'Nothing has been charged yet.'}
      </p>
      <Field label="Reason (optional)">
        <input
          className="input"
          placeholder="Plans changed"
          value={reason}
          onChange={(event) => setReason(event.target.value)}
        />
      </Field>
      {cancel.error && <Alert kind="error">{cancel.error}</Alert>}
      <div className="row end">
        <Button variant="secondary" onClick={onClose}>
          Keep booking
        </Button>
        <Button variant="danger" busy={cancel.busy} onClick={submit}>
          Cancel booking
        </Button>
      </div>
    </Dialog>
  );
}
