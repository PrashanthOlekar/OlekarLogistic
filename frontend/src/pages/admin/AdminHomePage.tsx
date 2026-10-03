import { Button, Loading, PageHead, Stat } from '../../components';
import { useLoad } from '../../hooks/useLoad';
import { api } from '../../lib/api';
import { inr } from '../../lib/format';
import { Link } from '../../lib/router';
import { BarList, DailyColumns } from './Charts';
import type { AdminSummary } from './types';

export function AdminHomePage() {
  const summary = useLoad(() => api<AdminSummary>('/admin/summary'));
  const s = summary.data;

  return (
    <>
      <PageHead title="Operations dashboard" sub="Live from the database. Times are in IST.">
        <Button variant="secondary" size="sm" onClick={summary.reload}>
          Refresh
        </Button>
      </PageHead>

      <Loading state={summary} />

      {s && (
        <>
          <div className="grid g4 mb-md">
            <Stat label="Bookings today" value={s.bookingsToday} />
            <Stat label="Active trips" value={s.activeTrips} />
            <Stat label="Completed this month" value={s.completedThisMonth} />
            <Stat label="Paid, waiting for a truck" value={s.awaitingTruck} alert={s.awaitingTruck > 0} />
            <Stat label="Collected this month" value={inr(s.revenueThisMonth)} note="Customer payments" />
            <Stat label="Commission this month" value={inr(s.commissionThisMonth)} />
            <Stat label="Unpaid quotes" value={s.awaitingPayment} />
            <FleetStat vehicles={s.vehicles} />
          </div>

          <div className="card mb-md">
            <h2 className="card-title">Needs action</h2>
            <div className="grid g4">
              <QueueLink
                to="/admin/approvals"
                label="Approvals"
                count={s.pendingApprovals}
                note="Owners, drivers, vehicles"
              />
              <QueueLink
                to="/admin/documents"
                label="Documents"
                count={s.pendingDocuments}
                note="Waiting for review"
              />
              <QueueLink
                to="/admin/trips"
                label="POD to approve"
                count={s.podToApprove}
                note="Delivered trips"
              />
              <QueueLink
                to="/admin/settlements"
                label="Payouts to send"
                count={s.settlementsToRelease}
                note="Owner settlements"
              />
            </div>
          </div>

          <div className="grid g3">
            <div className="card">
              <h2 className="card-title">Bookings, last 7 days</h2>
              <DailyColumns days={s.last7} />
            </div>
            <div className="card">
              <h2 className="card-title">Top routes</h2>
              <BarList rows={s.routes.map((row) => ({ label: row.route, count: row.count }))} />
            </div>
            <div className="card">
              <h2 className="card-title">Bookings by pickup city</h2>
              <BarList rows={s.cities.map((row) => ({ label: row.city, count: row.count }))} />
            </div>
          </div>
        </>
      )}
    </>
  );
}

function FleetStat({ vehicles }: { vehicles: AdminSummary['vehicles'] }) {
  const total = vehicles.reduce((sum, group) => sum + group.count, 0);
  const breakdown = vehicles.map((group) => `${group.count} ${group.status.toLowerCase()}`).join(' · ');
  return <Stat label="Approved vehicles" value={total} note={breakdown || 'None yet'} />;
}

function QueueLink({ to, label, count, note }: { to: string; label: string; count: number; note: string }) {
  return (
    <Link to={to} className="card stat">
      <small>{label}</small>
      <b>{count}</b>
      <span>{note}</span>
    </Link>
  );
}
