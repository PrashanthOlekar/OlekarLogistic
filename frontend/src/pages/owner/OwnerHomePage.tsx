import { useState } from 'react';
import { Link } from 'react-router';
import { dashboardsApi } from '../../api/adminApi';
import { driversApi } from '../../api/driversApi';
import { loadsApi } from '../../api/loadsApi';
import { Alert, Button, Empty, Loading, PageHead, RouteLabel, Stat, useToast } from '../../components';
import { TakeLoadDialog } from '../../features/fleet/TakeLoadDialog';
import { useAction } from '../../hooks/useAction';
import { useLoad } from '../../hooks/useLoad';
import { MAX_PAGE_SIZE, type AvailableLoad } from '../../types';
import { date, inr, kg, plural } from '../../utils/format';

export function OwnerHomePage() {
  const toast = useToast();
  const summary = useLoad(() => dashboardsApi.getOwner());
  const loads = useLoad(() => loadsApi.listAvailable());
  const drivers = useLoad(() => driversApi.list({ pageSize: MAX_PAGE_SIZE, dutyStatus: 'Available' }));
  const [taking, setTaking] = useState<AvailableLoad | null>(null);
  const decline = useAction();

  const declineLoad = (load: AvailableLoad) =>
    decline.run(async () => {
      await loadsApi.decline(load.id);
      loads.reload();
    });

  const handleTaken = (tripNumber: string) => {
    toast(`Load taken. Trip ${tripNumber} is assigned to your driver.`);
    setTaking(null);
    loads.reload();
    summary.reload();
    drivers.reload();
  };

  const s = summary.data;

  return (
    <>
      <PageHead
        title="Loads & overview"
        sub="Paid loads that match your verified, available trucks appear here."
      />

      <Loading state={summary} />

      {s && s.kycStatus !== 'Approved' && (
        <KycAlert kycStatus={s.kycStatus} rejectionReason={s.rejectionReason} />
      )}

      {s && (
        <div className="grid g4 mb-lg">
          <Stat label="Earned this month" value={inr(s.earnedThisMonth)} note="Paid to your bank" />
          <Stat label="Pending payout" value={inr(s.pendingPayout)} note="After delivery proof is approved" />
          <Stat label="Vehicles" value={`${s.vehiclesApproved}/${s.vehicles}`} note="Verified / total" />
          <Stat label="Active trips" value={s.activeTrips} note={`${plural(s.drivers, 'driver')} linked`} />
        </div>
      )}

      <div className="card">
        <div className="card-head">
          <h2>Available loads</h2>
          <Button variant="ghost" size="sm" onClick={() => loads.reload()}>
            Refresh
          </Button>
        </div>

        <Loading state={loads} />
        {decline.error && <Alert kind="error">{decline.error}</Alert>}

        {loads.data && !loads.data.kycApproved && <Empty title="Loads appear once your KYC is approved" />}

        {loads.data?.kycApproved && loads.data.loads.length === 0 && (
          <Empty title="No matching loads right now">
            Loads show up when a customer pays for a trip that fits one of your verified, available trucks.
            {s?.vehiclesApproved === 0 && (
              <>
                <br />
                <Link to="/owner/vehicles">Add a vehicle and its documents</Link> to get verified.
              </>
            )}
          </Empty>
        )}

        <div className="stack">
          {loads.data?.loads.map((load) => (
            <LoadCard
              key={load.id}
              load={load}
              onTake={() => setTaking(load)}
              onDecline={() => declineLoad(load)}
            />
          ))}
        </div>
      </div>

      {taking && (
        <TakeLoadDialog
          load={taking}
          drivers={drivers.data?.items ?? []}
          onClose={() => setTaking(null)}
          onTaken={handleTaken}
        />
      )}
    </>
  );
}

function KycAlert({ kycStatus, rejectionReason }: { kycStatus: string; rejectionReason?: string | null }) {
  const documentsLink = <Link to="/owner/documents">KYC documents</Link>;

  if (kycStatus === 'Rejected') {
    return (
      <Alert kind="error" title="Your KYC was not approved" className="mb-md">
        Reason: {rejectionReason}. Upload corrected documents in {documentsLink}.
      </Alert>
    );
  }

  return (
    <Alert kind="warn" title="Your KYC is under review" className="mb-md">
      Upload your Aadhaar, PAN and cancelled cheque in {documentsLink}. You can add vehicles and drivers
      meanwhile.
    </Alert>
  );
}

interface LoadCardProps {
  load: AvailableLoad;
  onTake: () => void;
  onDecline: () => void;
}

function LoadCard({ load, onTake, onDecline }: LoadCardProps) {
  const slot = load.pickupSlot ? `, ${load.pickupSlot}` : '';

  return (
    <div className="card load-card">
      <div className="row between">
        <RouteLabel from={load.from} to={load.to} />
        <b className="load-payout">{inr(load.payout)}</b>
      </div>
      <p className="muted small load-meta">
        {load.goods} · {kg(load.weightKg)} · {load.vehicleType} · {load.distanceKm} km · pickup{' '}
        {date(load.pickupDate)}
        {slot} · <span className="mono">{load.bookingNumber}</span>
      </p>
      <div className="row">
        <Button onClick={onTake}>Take this load</Button>
        <Button variant="ghost" onClick={onDecline}>
          Not interested
        </Button>
      </div>
    </div>
  );
}
