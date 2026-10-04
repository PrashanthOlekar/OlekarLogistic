import { Button, DocumentChips, Field, Pill } from '../../components';
import type { DriverListItem, VehicleListItem } from '../../types';
import { VEHICLE_DOCUMENTS, documentName } from '../../utils/documents';
import { kg } from '../../utils/format';

const AVAILABILITY_OPTIONS = ['Available', 'Busy', 'Maintenance'];

interface VehicleCardProps {
  vehicle: VehicleListItem;
  drivers: DriverListItem[];
  onAvailabilityChange: (status: string) => void;
  onDriverChange: (driverId: number) => void;
  onUpload: () => void;
}

export function VehicleCard({
  vehicle,
  drivers,
  onAvailabilityChange,
  onDriverChange,
  onUpload,
}: VehicleCardProps) {
  const missingDocuments = VEHICLE_DOCUMENTS.filter(
    (type) =>
      !vehicle.documents.some((document) => document.docType === type && document.status !== 'Rejected'),
  );
  const makeModel = vehicle.makeModel ? ` · ${vehicle.makeModel}` : '';

  return (
    <div className="card stack">
      <div className="row between">
        <div>
          <b className="plate">{vehicle.registrationNumber}</b>
          <div className="muted small">
            {vehicle.vehicleType} · {kg(vehicle.capacityKg)}
            {makeModel}
          </div>
        </div>
        <Pill status={vehicle.verificationStatus} />
      </div>

      <div className="seg" role="group" aria-label={`Availability of ${vehicle.registrationNumber}`}>
        {AVAILABILITY_OPTIONS.map((status) => (
          <button
            key={status}
            type="button"
            aria-pressed={vehicle.availabilityStatus === status}
            onClick={() => onAvailabilityChange(status)}
          >
            {status}
          </button>
        ))}
      </div>

      <Field label="Regular driver">
        <select
          className="input"
          value={vehicle.currentDriverId ?? 0}
          onChange={(event) => onDriverChange(Number(event.target.value))}
        >
          <option value={0}>Not set</option>
          {drivers.map((driver) => (
            <option key={driver.id} value={driver.id}>
              {driver.name}
            </option>
          ))}
        </select>
      </Field>

      <div className="stack tight">
        <small className="muted">Documents</small>
        {vehicle.documents.length > 0 && <DocumentChips documents={vehicle.documents} showStatusMark />}
        {missingDocuments.length > 0 && (
          <p className="small warn-text">Missing: {missingDocuments.map(documentName).join(', ')}</p>
        )}
      </div>

      <Button variant="secondary" size="sm" onClick={onUpload}>
        Upload document
      </Button>
    </div>
  );
}
