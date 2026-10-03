import { useEffect, useState, type FormEvent } from 'react';
import { Alert, Button, Dialog, Field } from '../../components';
import { useAction } from '../../hooks/useAction';
import { api } from '../../lib/api';
import { getMeta } from '../../lib/meta';
import type { VehicleType } from '../../lib/types';

/** The 14 ft truck is the most common choice, so it is selected first when available. */
const DEFAULT_TYPE_INDEX = 3;

interface AddVehicleDialogProps {
  onClose: () => void;
  onAdded: () => void;
}

export function AddVehicleDialog({ onClose, onAdded }: AddVehicleDialogProps) {
  const [vehicleTypes, setVehicleTypes] = useState<VehicleType[]>([]);
  const [registrationNumber, setRegistrationNumber] = useState('');
  const [vehicleTypeId, setVehicleTypeId] = useState(0);
  const [capacityKg, setCapacityKg] = useState(0);
  const [manufactureYear, setManufactureYear] = useState('');
  const [makeModel, setMakeModel] = useState('');
  const save = useAction();

  useEffect(() => {
    getMeta().then((meta) => {
      setVehicleTypes(meta.vehicleTypes);
      const first = meta.vehicleTypes[DEFAULT_TYPE_INDEX] ?? meta.vehicleTypes[0];
      setVehicleTypeId(first?.id ?? 0);
      setCapacityKg(first?.maxLoadKg ?? 0);
    });
  }, []);

  /** Changing the type also suggests that type's usual capacity. */
  const chooseType = (id: number) => {
    setVehicleTypeId(id);
    const type = vehicleTypes.find((option) => option.id === id);
    if (type) {
      setCapacityKg(type.maxLoadKg);
    }
  };

  const submit = (event: FormEvent) => {
    event.preventDefault();
    save.run(async () => {
      await api('/owner/vehicles', {
        body: {
          registrationNumber,
          vehicleTypeId,
          capacityKg,
          makeModel,
          manufactureYear: manufactureYear ? Number(manufactureYear) : null,
          homeCityId: null,
        },
      });
      onAdded();
    });
  };

  return (
    <Dialog title="Add a vehicle" onClose={onClose}>
      <form className="stack" onSubmit={submit}>
        <Field label="Registration number" hint="As printed on the RC">
          <input
            className="input"
            placeholder="KA 01 AB 4521"
            value={registrationNumber}
            onChange={(event) => setRegistrationNumber(event.target.value.toUpperCase())}
            autoFocus
          />
        </Field>

        <Field label="Vehicle type">
          <select
            className="input"
            value={vehicleTypeId}
            onChange={(event) => chooseType(Number(event.target.value))}
          >
            {vehicleTypes.map((type) => (
              <option key={type.id} value={type.id}>
                {type.name}
              </option>
            ))}
          </select>
        </Field>

        <div className="grid g2">
          <Field label="Load capacity (kg)">
            <input
              className="input"
              type="number"
              value={capacityKg}
              onChange={(event) => setCapacityKg(Number(event.target.value))}
            />
          </Field>
          <Field label="Year (optional)">
            <input
              className="input"
              inputMode="numeric"
              maxLength={4}
              value={manufactureYear}
              onChange={(event) => setManufactureYear(event.target.value)}
            />
          </Field>
        </div>

        <Field label="Make and model (optional)">
          <input
            className="input"
            placeholder="Tata 1109g LPT"
            value={makeModel}
            onChange={(event) => setMakeModel(event.target.value)}
          />
        </Field>

        {save.error && <Alert kind="error">{save.error}</Alert>}

        <Button type="submit" block busy={save.busy}>
          Add vehicle
        </Button>
      </form>
    </Dialog>
  );
}
