import { EmptyCard, Loading, PageHead, Pill } from '../../components';
import { useLoad } from '../../hooks/useLoad';
import { api } from '../../lib/api';
import { dateTime, inr } from '../../lib/format';
import type { OwnerPayout, OwnerSummary } from './types';

export function PayoutsPage() {
  const payouts = useLoad(() => api<OwnerPayout[]>('/owner/settlements'));
  const summary = useLoad(() => api<OwnerSummary>('/owner/summary'));

  const bank = summary.data?.bank;
  const subtitle = bank
    ? `Paid to ${bank.accountHolder}, account ending ${bank.accountLast4} (${bank.ifsc}), after the delivery proof is approved.`
    : 'Paid to your bank after the delivery proof is approved.';

  return (
    <>
      <PageHead title="Payouts" sub={subtitle} />

      <Loading state={payouts} />

      {payouts.data?.length === 0 && (
        <EmptyCard title="No payouts yet">
          Your first payout appears here after your first delivery.
        </EmptyCard>
      )}

      {!!payouts.data?.length && (
        <div className="card table-wrap">
          <table>
            <thead>
              <tr>
                <th>Trip</th>
                <th className="num">Freight</th>
                <th className="num">Commission</th>
                <th className="num">TDS</th>
                <th className="num">You receive</th>
                <th>Status</th>
                <th>Bank reference</th>
              </tr>
            </thead>
            <tbody>
              {payouts.data.map((payout) => (
                <tr key={payout.id}>
                  <td className="mono">{payout.trip}</td>
                  <td className="num">{inr(payout.grossAmount)}</td>
                  <td className="num">− {inr(payout.commissionAmount)}</td>
                  <td className="num">{payout.tdsAmount ? `− ${inr(payout.tdsAmount)}` : '—'}</td>
                  <td className="num">
                    <b>{inr(payout.netAmount)}</b>
                  </td>
                  <td>
                    <Pill status={payout.status} />
                  </td>
                  <td className="mono small">
                    {payout.utr ? `${payout.utr} · ${dateTime(payout.releasedAt)}` : '—'}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </>
  );
}
