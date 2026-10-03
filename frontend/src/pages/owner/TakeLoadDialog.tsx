import { useState } from 'react';
import { Alert, Button, Dialog, Field } from '../../components';
import { useAction } from '../../hooks/useAction';
import { api } from '../../lib/api';
import { inr } from '../../lib/format';
import { freeDrivers, type AvailableLoad, type OwnerDriver } from './types';

interface TakeLoadDialogProps {
  load: AvailableLoad;
  drivers: OwnerDriver[];
  onClose: () => void;
  onTaken: (tripNumber: string) => void;
}

/** Pick which of my trucks and drivers will carry the load. */
export function TakeLoadDialog({ load, drivers, onClose, onTaken }: TakeLoadDialogProps) {
  const available = freeDrivers(drivers);
  const firstVehicle = load.vehicles[0];

  // Start with the first truck and, if free, its regular driver.
  const [vehicleId, setVehicleId] = useState(firstVehicle?.id ?? 0);
  const [driverId, setDriverId] = useState(() => {
    const regularDriverId = firstVehicle?.currentDriverId;
    const regularIsFree = available.some((driver) => driver.id === regularDriverId);
    return regularDriverId && regularIsFree ? regularDriverId : (available[0]?.id ?? 0);
  });
  const accept = useAction();

  const submit = () =>
    accept.run(async () => {
      const result = await api<{ tripNumber: string }>(`/owner/loads/${load.id}/accept`, {
        body: { vehicleId, driverId },
      });
      onTaken(result.tripNumber);
    });

  const noDriverMessage =
    available.length === 0
      ? 'No verified driver is free. Ask a driver to register with your mobile number.'
      : null;

  return (
    <Dialog title={`Take ${load.from} → ${load.to}`} onClose={onClose}>
      <p className="muted">
        You'll receive <span className="strong">{inr(load.payout)}</span> after delivery is confirmed (freight
        minus ProCargo's commission).
      </p>

      <Field label="Vehicle">
        <select
          className="input"
          value={vehicleId}
          onChange={(event) => setVehicleId(Number(event.target.value))}
        >
          {load.vehicles.map((vehicle) => (
            <option key={vehicle.id} value={vehicle.id}>
              {vehicle.registrationNumber}
            </option>
          ))}
        </select>
      </Field>

      <Field label="Driver" error={noDriverMessage}>
        <select
          className="input"
          value={driverId}
          onChange={(event) => setDriverId(Number(event.target.value))}
        >
          {available.map((driver) => (
            <option key={driver.id} value={driver.id}>
              {driver.name} · {driver.mobile}
            </option>
          ))}
        </select>
      </Field>

      {accept.error && <Alert kind="error">{accept.error}</Alert>}

      <Button size="lg" block busy={accept.busy} disabled={!vehicleId || !driverId} onClick={submit}>
        Confirm and assign
      </Button>
    </Dialog>
  );
}
