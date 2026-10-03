import { useState } from 'react';
import { Alert, Button, Dialog, Field, Loading } from '../../components';
import { useAction } from '../../hooks/useAction';
import { useLoad } from '../../hooks/useLoad';
import { api } from '../../lib/api';
import { kg } from '../../lib/format';
import type { AdminBooking, AssignableTruck } from './types';

interface AssignTruckDialogProps {
  booking: AdminBooking;
  onClose: () => void;
  onAssigned: (tripNumber: string) => void;
}

/** Lets operations assign a truck by hand when no owner has taken a paid load. */
export function AssignTruckDialog({ booking, onClose, onAssigned }: AssignTruckDialogProps) {
  const [vehicleId, setVehicleId] = useState(0);
  const [driverId, setDriverId] = useState(0);
  const assign = useAction();

  const trucks = useLoad(async () => {
    const options = await api<AssignableTruck[]>(`/admin/bookings/${booking.id}/assignable`);
    setVehicleId(options[0]?.id ?? 0);
    setDriverId(options[0]?.drivers[0]?.id ?? 0);
    return options;
  }, [booking.id]);

  const chosenTruck = trucks.data?.find((truck) => truck.id === vehicleId);

  /** Switching truck picks that owner's first free driver. */
  const chooseTruck = (id: number) => {
    setVehicleId(id);
    setDriverId(trucks.data?.find((truck) => truck.id === id)?.drivers[0]?.id ?? 0);
  };

  const submit = () =>
    assign.run(async () => {
      const result = await api<{ tripNumber: string }>(`/admin/bookings/${booking.id}/assign`, {
        body: { vehicleId, driverId },
      });
      onAssigned(result.tripNumber);
    });

  const noDriverMessage =
    chosenTruck && chosenTruck.drivers.length === 0
      ? 'This owner has no verified driver free right now.'
      : null;

  return (
    <Dialog title={`Assign ${booking.bookingNumber}`} onClose={onClose}>
      <p className="muted">
        {booking.from} → {booking.to} · {booking.vehicleType} · {kg(booking.weightKg)}
      </p>

      <Loading state={trucks} />

      {trucks.data?.length === 0 && (
        <Alert kind="warn" title="No verified, free vehicle of this type">
          Approve more vehicles or ask owners to mark trucks Available.
        </Alert>
      )}

      {!!trucks.data?.length && (
        <>
          <Field label="Vehicle">
            <select
              className="input"
              value={vehicleId}
              onChange={(event) => chooseTruck(Number(event.target.value))}
            >
              {trucks.data.map((truck) => (
                <option key={truck.id} value={truck.id}>
                  {truck.registrationNumber} · {truck.owner}
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
              {chosenTruck?.drivers.map((driver) => (
                <option key={driver.id} value={driver.id}>
                  {driver.name}
                </option>
              ))}
            </select>
          </Field>
        </>
      )}

      {assign.error && <Alert kind="error">{assign.error}</Alert>}

      <Button block busy={assign.busy} disabled={!vehicleId || !driverId} onClick={submit}>
        Assign
      </Button>
    </Dialog>
  );
}
