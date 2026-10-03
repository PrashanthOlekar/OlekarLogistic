import { EmptyCard, Loading, PageHead, Pill } from '../../components';
import { useLoad } from '../../hooks/useLoad';
import { api } from '../../lib/api';
import { dateTime, inr } from '../../lib/format';
import type { AdminPayment } from './types';

export function PaymentsPage() {
  const payments = useLoad(() => api<AdminPayment[]>('/admin/payments'));

  return (
    <>
      <PageHead title="Payments" sub="Customer payments. Money is held until delivery is confirmed." />

      <Loading state={payments} />

      {payments.data?.length === 0 && <EmptyCard title="No payments yet" />}

      {!!payments.data?.length && (
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
              {payments.data.map((payment) => (
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
        </div>
      )}
    </>
  );
}
