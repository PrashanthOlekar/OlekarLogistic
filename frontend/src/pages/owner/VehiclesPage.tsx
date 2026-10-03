import { useState } from 'react';
import { Alert, Button, EmptyCard, Loading, PageHead, UploadDialog } from '../../components';
import { useAction } from '../../hooks/useAction';
import { useLoad } from '../../hooks/useLoad';
import { api } from '../../lib/api';
import { VEHICLE_DOCUMENTS } from '../../lib/documents';
import { useToast } from '../../state/ToastContext';
import { AddVehicleDialog } from './AddVehicleDialog';
import { VehicleCard } from './VehicleCard';
import type { OwnerDriver, OwnerVehicle } from './types';

export function VehiclesPage() {
  const toast = useToast();
  const vehicles = useLoad(() => api<OwnerVehicle[]>('/owner/vehicles'));
  const drivers = useLoad(() => api<OwnerDriver[]>('/owner/drivers'));
  const [adding, setAdding] = useState(false);
  const [uploadFor, setUploadFor] = useState<OwnerVehicle | null>(null);
  const update = useAction();

  const setAvailability = (vehicle: OwnerVehicle, status: string) =>
    update.run(async () => {
      await api(`/owner/vehicles/${vehicle.id}/availability`, { method: 'PATCH', body: { status } });
      vehicles.reload();
    });

  const setRegularDriver = (vehicle: OwnerVehicle, driverId: number) =>
    update.run(async () => {
      await api(`/owner/vehicles/${vehicle.id}/driver`, {
        method: 'PATCH',
        body: { driverId: driverId || null },
      });
      vehicles.reload();
    });

  const handleAdded = () => {
    toast('Vehicle added. Upload its documents to get it verified.');
    setAdding(false);
    vehicles.reload();
  };

  const handleUploaded = () => {
    setUploadFor(null);
    toast('Document uploaded for review.');
    vehicles.reload();
  };

  return (
    <>
      <PageHead
        title="Vehicles"
        sub="Each vehicle needs its RC, insurance, fitness, permit and PUC verified before it gets loads."
      >
        <Button onClick={() => setAdding(true)}>Add vehicle</Button>
      </PageHead>

      {update.error && (
        <Alert kind="error" className="mb-sm">
          {update.error}
        </Alert>
      )}

      <Loading state={vehicles} />

      {vehicles.data?.length === 0 && (
        <EmptyCard title="No vehicles yet">Add your first lorry to start receiving loads.</EmptyCard>
      )}

      <div className="grid g2">
        {vehicles.data?.map((vehicle) => (
          <VehicleCard
            key={vehicle.id}
            vehicle={vehicle}
            drivers={drivers.data ?? []}
            onAvailabilityChange={(status) => setAvailability(vehicle, status)}
            onDriverChange={(driverId) => setRegularDriver(vehicle, driverId)}
            onUpload={() => setUploadFor(vehicle)}
          />
        ))}
      </div>

      {adding && <AddVehicleDialog onClose={() => setAdding(false)} onAdded={handleAdded} />}

      {uploadFor && (
        <UploadDialog
          title={`Upload for ${uploadFor.registrationNumber}`}
          entityType="Vehicle"
          entityId={uploadFor.id}
          docTypes={[...VEHICLE_DOCUMENTS, 'Other']}
          onClose={() => setUploadFor(null)}
          onDone={handleUploaded}
        />
      )}
    </>
  );
}
