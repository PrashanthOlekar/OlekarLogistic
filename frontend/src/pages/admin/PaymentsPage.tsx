import { useState } from 'react';
import { paymentsApi } from '../../api/adminApi';
import { EmptyCard, Loading, PageHead, Pagination, Pill } from '../../components';
import { SearchBox } from '../../features/admin/SearchBox';
import { usePagedLoad } from '../../hooks/usePagedLoad';
import { dateTime, inr } from '../../utils/format';

export function PaymentsPage() {
  const [search, setSearch] = useState('');
  const payments = usePagedLoad((page) => paymentsApi.list({ ...page, search }), [search]);

  return (
    <>
      <PageHead title="Payments" sub="Customer payments. Money is held until delivery is confirmed.">
        <SearchBox placeholder="Booking no. or customer" onSearch={setSearch} />
      </PageHead>

      <Loading state={payments} />

      {payments.items?.length === 0 && <EmptyCard title="No payments yet" />}

      {!!payments.items?.length && (
        <div className="card table-wrap">
          <table>
            <thead>
              <tr>
                <th>Paid</th>
                <th>Booking</th>
                <th>Customer</th>
                <th>Method</th>
                <th>Reference</th>
                <th className="num">Amount</th>
                <th>Status</th>
              </tr>
            </thead>
            <tbody>
              {payments.items.map((payment) => (
                <tr key={payment.id}>
                  <td>{dateTime(payment.paidAt)}</td>
                  <td className="mono">{payment.bookingNumber}</td>
                  <td>{payment.customer}</td>
                  <td>
                    {payment.method}
                    {payment.gateway === 'Test' && <span className="muted small"> · test</span>}
                  </td>
                  <td className="mono small">{payment.gatewayPaymentId}</td>
                  <td className="num">{inr(payment.amount)}</td>
                  <td>
                    <Pill status={payment.status} />
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
          <Pagination page={payments.data} onPageChange={payments.setPageNumber} />
        </div>
      )}
    </>
  );
}
