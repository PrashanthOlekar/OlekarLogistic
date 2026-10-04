import { useState } from 'react';
import { driversApi } from '../../api/driversApi';
import { vehiclesApi } from '../../api/vehiclesApi';
import { Alert, Button, EmptyCard, Loading, PageHead, Pagination, UploadDialog, useToast } from '../../components';
import { AddVehicleDialog } from '../../features/fleet/AddVehicleDialog';
import { VehicleCard } from '../../features/fleet/VehicleCard';
import { useAction } from '../../hooks/useAction';
import { useLoad } from '../../hooks/useLoad';
import { usePagedLoad } from '../../hooks/usePagedLoad';
import { MAX_PAGE_SIZE, type VehicleListItem } from '../../types';
import { VEHICLE_DOCUMENTS } from '../../utils/documents';

export function VehiclesPage() {
  const toast = useToast();
  const vehicles = usePagedLoad((page) => vehiclesApi.list(page));
  const drivers = useLoad(() => driversApi.list({ pageSize: MAX_PAGE_SIZE }));
  const [adding, setAdding] = useState(false);
  const [uploadFor, setUploadFor] = useState<VehicleListItem | null>(null);
  const update = useAction();

  const setAvailability = (vehicle: VehicleListItem, status: string) =>
    update.run(async () => {
      await vehiclesApi.update(vehicle.id, { availabilityStatus: status });
      vehicles.reload();
    });

  const setRegularDriver = (vehicle: VehicleListItem, driverId: number) =>
    update.run(async () => {
      await vehiclesApi.update(vehicle.id, driverId ? { currentDriverId: driverId } : { removeDriver: true });
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

      {vehicles.items?.length === 0 && (
        <EmptyCard title="No vehicles yet">Add your first lorry to start receiving loads.</EmptyCard>
      )}

      <div className="grid g2">
        {vehicles.items?.map((vehicle) => (
          <VehicleCard
            key={vehicle.id}
            vehicle={vehicle}
            drivers={drivers.data?.items ?? []}
            onAvailabilityChange={(status) => setAvailability(vehicle, status)}
            onDriverChange={(driverId) => setRegularDriver(vehicle, driverId)}
            onUpload={() => setUploadFor(vehicle)}
          />
        ))}
      </div>
      <Pagination page={vehicles.data} onPageChange={vehicles.setPageNumber} standalone />

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
