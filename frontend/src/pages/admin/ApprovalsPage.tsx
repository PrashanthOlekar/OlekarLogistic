import { useState } from 'react';
import { approvalsApi } from '../../api/adminApi';
import { driversApi, ownersApi } from '../../api/driversApi';
import { vehiclesApi } from '../../api/vehiclesApi';
import {
  Alert,
  Button,
  DocumentChips,
  EmptyCard,
  Loading,
  PageHead,
  ReasonDialog,
  useToast,
} from '../../components';
import { useAction } from '../../hooks/useAction';
import { useLoad } from '../../hooks/useLoad';
import type { VerificationRequest } from '../../types';
import { date, kg } from '../../utils/format';

type Tab = 'owners' | 'drivers' | 'vehicles';
type ApplicantKind = 'owner' | 'driver' | 'vehicle';

const TABS: { id: Tab; label: string }[] = [
  { id: 'owners', label: 'Owners' },
  { id: 'drivers', label: 'Drivers' },
  { id: 'vehicles', label: 'Vehicles' },
];

/** The verification endpoint for each kind of applicant. */
const VERIFY: Record<ApplicantKind, (id: number, request: VerificationRequest) => Promise<void>> = {
  owner: ownersApi.setVerification,
  driver: driversApi.setVerification,
  vehicle: vehiclesApi.setVerification,
};

interface Rejecting {
  kind: ApplicantKind;
  id: number;
  name: string;
}

export function ApprovalsPage() {
  const toast = useToast();
  const approvals = useLoad(() => approvalsApi.getPending());
  const [tab, setTab] = useState<Tab>('owners');
  const [rejecting, setRejecting] = useState<Rejecting | null>(null);
  const review = useAction();

  const decide = (kind: ApplicantKind, id: number, approve: boolean, reason?: string) =>
    review.run(async () => {
      await VERIFY[kind](id, { approve, reason });
      toast(approve ? 'Approved.' : 'Rejected and the applicant has been told why.');
      setRejecting(null);
      approvals.reload();
    });

  /** Approve and Reject buttons for one row. */
  const decisionButtons = (kind: ApplicantKind, id: number, name: string) => (
    <div className="row tight">
      <Button size="sm" variant="success" busy={review.busy} onClick={() => decide(kind, id, true)}>
        Approve
      </Button>
      <Button size="sm" variant="danger" onClick={() => setRejecting({ kind, id, name })}>
        Reject
      </Button>
    </div>
  );

  const data = approvals.data;

  return (
    <>
      <PageHead
        title="Approvals"
        sub="Open each document before approving. Approved owners, drivers and vehicles can start taking trips."
      />

      {review.error && (
        <Alert kind="error" className="mb-sm">
          {review.error}
        </Alert>
      )}

      <div className="tabs" role="tablist">
        {TABS.map((option) => (
          <button
            key={option.id}
            role="tab"
            aria-selected={tab === option.id}
            onClick={() => setTab(option.id)}
          >
            {option.label} {data ? `(${data[option.id].length})` : ''}
          </button>
        ))}
      </div>

      <Loading state={approvals} />

      {data &&
        tab === 'owners' &&
        (data.owners.length === 0 ? (
          <EmptyCard title="No owners waiting" />
        ) : (
          <div className="card table-wrap">
            <table>
              <thead>
                <tr>
                  <th>Owner</th>
                  <th>KYC</th>
                  <th>Bank</th>
                  <th>Documents</th>
                  <th>Applied</th>
                  <th />
                </tr>
              </thead>
              <tbody>
                {data.owners.map((owner) => (
                  <tr key={owner.id}>
                    <td>
                      <b>{owner.name}</b>
                      <div className="muted small">
                        {owner.businessName} <span className="mono">{owner.mobile}</span>
                      </div>
                    </td>
                    <td className="small">
                      PAN ••{owner.panLast4}
                      <br />
                      Aadhaar ••{owner.aadhaarLast4}
                    </td>
                    <td className="mono small">{owner.bank}</td>
                    <td>
                      <DocumentChips documents={owner.documents} />
                    </td>
                    <td className="small">{date(owner.createdAt)}</td>
                    <td>{decisionButtons('owner', owner.id, owner.name)}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        ))}

      {data &&
        tab === 'drivers' &&
        (data.drivers.length === 0 ? (
          <EmptyCard title="No drivers waiting" />
        ) : (
          <div className="card table-wrap">
            <table>
              <thead>
                <tr>
                  <th>Driver</th>
                  <th>Licence</th>
                  <th>Owner</th>
                  <th>Documents</th>
                  <th />
                </tr>
              </thead>
              <tbody>
                {data.drivers.map((driver) => (
                  <tr key={driver.id}>
                    <td>
                      <b>{driver.name}</b>
                      <div className="mono small muted">{driver.mobile}</div>
                    </td>
                    <td className="small">
                      <span className="mono">{driver.licenceNumber}</span>
                      <br />
                      {driver.licenceClass} · until {date(driver.licenceExpiry)}
                    </td>
                    <td>{driver.owner ?? <span className="muted">Own vehicle</span>}</td>
                    <td>
                      <DocumentChips documents={driver.documents} />
                    </td>
                    <td>{decisionButtons('driver', driver.id, driver.name)}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        ))}

      {data &&
        tab === 'vehicles' &&
        (data.vehicles.length === 0 ? (
          <EmptyCard title="No vehicles waiting" />
        ) : (
          <div className="card table-wrap">
            <table>
              <thead>
                <tr>
                  <th>Vehicle</th>
                  <th>Owner</th>
                  <th>Documents</th>
                  <th />
                </tr>
              </thead>
              <tbody>
                {data.vehicles.map((vehicle) => (
                  <tr key={vehicle.id}>
                    <td>
                      <b className="mono">{vehicle.registrationNumber}</b>
                      <div className="muted small">
                        {vehicle.vehicleType} · {kg(vehicle.capacityKg)}
                      </div>
                    </td>
                    <td>{vehicle.owner}</td>
                    <td>
                      <DocumentChips documents={vehicle.documents} />
                    </td>
                    <td>{decisionButtons('vehicle', vehicle.id, vehicle.registrationNumber)}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        ))}

      {rejecting && (
        <ReasonDialog
          title={`Reject ${rejecting.name}`}
          busy={review.busy}
          onClose={() => setRejecting(null)}
          onSubmit={(reason) => decide(rejecting.kind, rejecting.id, false, reason)}
        />
      )}
    </>
  );
}
