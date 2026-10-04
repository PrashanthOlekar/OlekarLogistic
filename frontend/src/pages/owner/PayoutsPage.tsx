import { dashboardsApi } from '../../api/adminApi';
import { settlementsApi } from '../../api/settlementsApi';
import { EmptyCard, Loading, PageHead, Pagination, Pill } from '../../components';
import { useLoad } from '../../hooks/useLoad';
import { usePagedLoad } from '../../hooks/usePagedLoad';
import { dateTime, inr } from '../../utils/format';

export function PayoutsPage() {
  const payouts = usePagedLoad((page) => settlementsApi.list(page));
  const summary = useLoad(() => dashboardsApi.getOwner());

  const bank = summary.data?.bank;
  const subtitle = bank
    ? `Paid to ${bank.accountHolder}, account ending ${bank.accountLast4} (${bank.ifsc}), after the delivery proof is approved.`
    : 'Paid to your bank after the delivery proof is approved.';

  return (
    <>
      <PageHead title="Payouts" sub={subtitle} />

      <Loading state={payouts} />

      {payouts.items?.length === 0 && (
        <EmptyCard title="No payouts yet">
          Your first payout appears here after your first delivery.
        </EmptyCard>
      )}

      {!!payouts.items?.length && (
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
              {payouts.items.map((payout) => (
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
          <Pagination page={payouts.data} onPageChange={payouts.setPageNumber} />
        </div>
      )}
    </>
  );
}
