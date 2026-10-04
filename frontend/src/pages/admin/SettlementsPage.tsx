import { useState } from 'react';
import {
  Alert,
  Button,
  Dialog,
  EmptyCard,
  Field,
  Loading,
  MoneyRow,
  MoneyRows,
  PageHead,
  Pagination,
  Pill,
  useToast,
} from '../../components';
import { settlementsApi } from '../../api/settlementsApi';
import { StatusFilter } from '../../features/admin/StatusFilter';
import { useAction } from '../../hooks/useAction';
import { usePagedLoad } from '../../hooks/usePagedLoad';
import type { SettlementListItem } from '../../types';
import { inr } from '../../utils/format';

const FILTERS: [string, string][] = [
  ['Approved', 'Ready to pay'],
  ['AwaitingPod', 'Waiting for POD'],
  ['Released', 'Paid'],
  ['', 'All'],
];

export function SettlementsPage() {
  const toast = useToast();
  const [status, setStatus] = useState('Approved');
  const settlements = usePagedLoad((page) => settlementsApi.list({ ...page, status }), [status]);
  const [releasing, setReleasing] = useState<SettlementListItem | null>(null);

  const handleReleased = (settlement: SettlementListItem) => {
    toast(`${inr(settlement.netAmount)} recorded as paid to ${settlement.owner}.`);
    setReleasing(null);
    settlements.reload();
  };

  return (
    <>
      <PageHead
        title="Owner payouts"
        sub="Send the amount from your bank or gateway payout dashboard, then record the UTR here."
      >
        <StatusFilter options={FILTERS} value={status} onChange={setStatus} />
      </PageHead>

      <Loading state={settlements} />

      {settlements.items?.length === 0 && <EmptyCard title="Nothing here" />}

      {!!settlements.items?.length && (
        <div className="card table-wrap">
          <table>
            <thead>
              <tr>
                <th>Trip</th>
                <th>Owner</th>
                <th>Bank</th>
                <th className="num">Freight</th>
                <th className="num">Commission</th>
                <th className="num">Pay owner</th>
                <th>Status</th>
                <th />
              </tr>
            </thead>
            <tbody>
              {settlements.items.map((settlement) => (
                <tr key={settlement.id}>
                  <td className="mono">{settlement.trip}</td>
                  <td>{settlement.owner}</td>
                  <td className="mono small">{settlement.bank ?? '—'}</td>
                  <td className="num">{inr(settlement.grossAmount)}</td>
                  <td className="num">{inr(settlement.commissionAmount)}</td>
                  <td className="num">
                    <b>{inr(settlement.netAmount)}</b>
                  </td>
                  <td>
                    <Pill status={settlement.status} />
                    {settlement.utr && <div className="mono small muted">{settlement.utr}</div>}
                  </td>
                  <td>
                    {settlement.status === 'Approved' && (
                      <Button size="sm" variant="success" onClick={() => setReleasing(settlement)}>
                        Mark paid
                      </Button>
                    )}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
          <Pagination page={settlements.data} onPageChange={settlements.setPageNumber} />
        </div>
      )}

      {releasing && (
        <ReleasePayoutDialog
          settlement={releasing}
          onClose={() => setReleasing(null)}
          onReleased={() => handleReleased(releasing)}
        />
      )}
    </>
  );
}

interface ReleasePayoutDialogProps {
  settlement: SettlementListItem;
  onClose: () => void;
  onReleased: () => void;
}

/** Records a payout already sent from the bank, using its UTR (transfer reference). */
function ReleasePayoutDialog({ settlement, onClose, onReleased }: ReleasePayoutDialogProps) {
  const [utr, setUtr] = useState('');
  const release = useAction();

  const submit = () =>
    release.run(async () => {
      await settlementsApi.recordPayout(settlement.id, utr.trim());
      onReleased();
    });

  return (
    <Dialog title={`Pay ${settlement.owner}`} onClose={onClose}>
      <MoneyRows>
        <MoneyRow label="Freight" value={inr(settlement.grossAmount)} />
        <MoneyRow label="ProCargo commission" value={`− ${inr(settlement.commissionAmount)}`} />
        <MoneyRow label={`Send to ${settlement.bank}`} value={inr(settlement.netAmount)} total />
      </MoneyRows>

      <Field label="Bank transfer reference (UTR)">
        <input
          className="input mono"
          value={utr}
          onChange={(event) => setUtr(event.target.value.toUpperCase())}
          autoFocus
        />
      </Field>

      {release.error && <Alert kind="error">{release.error}</Alert>}

      <Button block busy={release.busy} disabled={!utr.trim()} onClick={submit}>
        Record payout
      </Button>
    </Dialog>
  );
}
