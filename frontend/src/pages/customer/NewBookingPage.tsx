import { useEffect, useState, type ChangeEvent, type FormEvent } from 'react';
import { useNavigate } from 'react-router';
import { bookingsApi } from '../../api/bookingsApi';
import { toApiError } from '../../api/errors';
import { referenceDataApi } from '../../api/referenceDataApi';
import { Alert, Button, Field, MoneyRow, MoneyRows, PageHead } from '../../components';
import { useAction } from '../../hooks/useAction';
import { getReferenceData } from '../../services/referenceData';
import type { PriceEstimate, ReferenceData } from '../../types';
import { inr, kg, plural, today } from '../../utils/format';

const PICKUP_SLOTS = ['Morning (6–10 am)', 'Midday (10 am–2 pm)', 'Afternoon (2–6 pm)', 'Night (after 8 pm)'];

/** Wait this long after the last change before asking the server for a new price. */
const ESTIMATE_DELAY_MS = 250;

const EMPTY_BOOKING = {
  pickupCityId: 0,
  pickupAddress: '',
  pickupContactName: '',
  pickupContactPhone: '',
  dropCityId: 0,
  dropAddress: '',
  dropContactName: '',
  dropContactPhone: '',
  goodsCategoryId: 0,
  goodsDescription: '',
  weightKg: 1000,
  goodsValue: '',
  vehicleTypeId: 0,
  pickupDate: today(),
  pickupSlot: PICKUP_SLOTS[0],
  specialInstructions: '',
};

type BookingForm = typeof EMPTY_BOOKING;
type InputEvent = ChangeEvent<HTMLInputElement | HTMLSelectElement | HTMLTextAreaElement>;

export function NewBookingPage() {
  const navigate = useNavigate();
  const [meta, setMeta] = useState<ReferenceData | null>(null);
  const [form, setForm] = useState<BookingForm>(EMPTY_BOOKING);
  const [estimate, setEstimate] = useState<PriceEstimate | null>(null);
  const [estimateError, setEstimateError] = useState<string | null>(null);
  const save = useAction();

  /** Binds a text input to a form field. */
  const text = (key: keyof BookingForm) => ({
    value: form[key],
    onChange: (event: InputEvent) => setForm((current) => ({ ...current, [key]: event.target.value })),
  });

  /** Binds a numeric input or an id <select> to a form field. */
  const number = (key: keyof BookingForm) => ({
    value: form[key],
    onChange: (event: InputEvent) =>
      setForm((current) => ({ ...current, [key]: Number(event.target.value) })),
  });

  // Load the lists and pre-fill a common route: Bengaluru → Hubballi in a 14 ft truck.
  useEffect(() => {
    getReferenceData()
      .then((loaded) => {
        setMeta(loaded);
        setForm((current) => ({ ...current, ...defaultChoices(loaded) }));
      })
      .catch((error) => setEstimateError(toApiError(error).message));
  }, []);

  // Live price: the server re-quotes whenever the route, vehicle or weight changes.
  useEffect(() => {
    const { pickupCityId, dropCityId, vehicleTypeId, weightKg } = form;
    if (!pickupCityId || !dropCityId || !vehicleTypeId || !weightKg) {
      return;
    }

    // Cancel the previous request when the form changes again, so an old price never wins.
    const controller = new AbortController();
    const timer = setTimeout(() => {
      referenceDataApi
        .estimatePrice({ pickupCityId, dropCityId, vehicleTypeId, weightKg }, controller.signal)
        .then((result) => {
          setEstimate(result);
          setEstimateError(null);
        })
        .catch((error) => {
          if (!controller.signal.aborted) {
            setEstimateError(toApiError(error).message);
          }
        });
    }, ESTIMATE_DELAY_MS);

    return () => {
      clearTimeout(timer);
      controller.abort();
    };
  }, [form.pickupCityId, form.dropCityId, form.vehicleTypeId, form.weightKg]);

  const submit = (event: FormEvent) => {
    event.preventDefault();
    save.run(async () => {
      const created = await bookingsApi.create({
        ...form,
        goodsValue: form.goodsValue ? Number(form.goodsValue) : null,
        pickupContactName: form.pickupContactName || null,
        pickupContactPhone: form.pickupContactPhone || null,
        dropContactName: form.dropContactName || null,
        dropContactPhone: form.dropContactPhone || null,
        specialInstructions: form.specialInstructions || null,
      });
      navigate(`/customer/bookings/${created.id}`);
    });
  };

  const selectedVehicle = meta?.vehicleTypes.find((type) => type.id === form.vehicleTypeId);
  const vehicleHint = selectedVehicle
    ? `${selectedVehicle.lengthFt} × ${selectedVehicle.widthFt} ft · up to ${kg(selectedVehicle.maxLoadKg)} · good for ${selectedVehicle.recommendedGoods?.toLowerCase()}`
    : undefined;

  const cityOptions = meta?.cities.map((city) => (
    <option key={city.id} value={city.id}>
      {city.name}, {city.state}
    </option>
  ));

  return (
    <>
      <PageHead
        title="Book a truck"
        sub="Tell us the route and the load. The price updates as you fill in the form."
      />

      <form className="grid split" onSubmit={submit}>
        <div className="card">
          <div className="form-section">
            <h3>
              <span className="n">1</span>Pickup
            </h3>
            <div className="grid g2">
              <Field label="City">
                <select className="input" {...number('pickupCityId')}>
                  {cityOptions}
                </select>
              </Field>
              <Field label="Date">
                <input className="input" type="date" min={today()} {...text('pickupDate')} />
              </Field>
            </div>
            <Field label="Address">
              <input
                className="input"
                placeholder="Warehouse 12, Peenya Industrial Area"
                {...text('pickupAddress')}
              />
            </Field>
            <div className="grid g2">
              <Field label="Contact at pickup (optional)">
                <input className="input" {...text('pickupContactName')} />
              </Field>
              <Field label="Their phone (optional)">
                <input className="input" inputMode="tel" {...text('pickupContactPhone')} />
              </Field>
            </div>
            <Field label="Time slot">
              <select className="input" {...text('pickupSlot')}>
                {PICKUP_SLOTS.map((slot) => (
                  <option key={slot}>{slot}</option>
                ))}
              </select>
            </Field>
          </div>

          <div className="form-section">
            <h3>
              <span className="n">2</span>Delivery
            </h3>
            <Field label="City">
              <select className="input" {...number('dropCityId')}>
                {cityOptions}
              </select>
            </Field>
            <Field label="Address">
              <input className="input" placeholder="Gokul Road, Hubballi" {...text('dropAddress')} />
            </Field>
            <div className="grid g2">
              <Field label="Receiver name (optional)">
                <input className="input" {...text('dropContactName')} />
              </Field>
              <Field label="Receiver phone (optional)">
                <input className="input" inputMode="tel" {...text('dropContactPhone')} />
              </Field>
            </div>
          </div>

          <div className="form-section">
            <h3>
              <span className="n">3</span>Goods and vehicle
            </h3>
            <div className="grid g2">
              <Field label="Type of goods">
                <select className="input" {...number('goodsCategoryId')}>
                  {meta?.goods.map((category) => (
                    <option key={category.id} value={category.id}>
                      {category.name}
                    </option>
                  ))}
                </select>
              </Field>
              <Field label="Approximate weight (kg)">
                <input className="input" type="number" min={1} {...number('weightKg')} />
              </Field>
            </div>
            <Field label="What are you sending?">
              <input className="input" placeholder="120 cartons of biscuits" {...text('goodsDescription')} />
            </Field>
            <Field label="Vehicle type" hint={vehicleHint}>
              <select className="input" {...number('vehicleTypeId')}>
                {meta?.vehicleTypes.map((type) => (
                  <option key={type.id} value={type.id}>
                    {type.name} · up to {kg(type.maxLoadKg)}
                  </option>
                ))}
              </select>
            </Field>
            <div className="grid g2">
              <Field label="Value of goods, ₹ (optional)" hint="Needed for an e-way bill above ₹50,000">
                <input className="input" type="number" min={0} {...text('goodsValue')} />
              </Field>
            </div>
            <Field label="Special instructions (optional)">
              <textarea
                className="input"
                placeholder="Fragile, needs tarpaulin, unloading at 2nd floor…"
                {...text('specialInstructions')}
              />
            </Field>
          </div>
        </div>

        <aside className="card card-sticky">
          <div className="card-head">
            <h2>Your price</h2>
            {estimate && <span className="pill green">Live</span>}
          </div>

          {estimateError && <Alert kind="error">{estimateError}</Alert>}

          {estimate?.overCapacity && (
            <Alert kind="warn" title={`Too heavy for this vehicle (max ${kg(estimate.maxLoadKg)})`}>
              {estimate.suggested && (
                <Button
                  size="sm"
                  variant="secondary"
                  className="mt-sm"
                  onClick={() =>
                    setForm((current) => ({ ...current, vehicleTypeId: estimate.suggested!.id }))
                  }
                >
                  Switch to {estimate.suggested.name}
                </Button>
              )}
            </Alert>
          )}

          {estimate && (
            <MoneyRows>
              <MoneyRow label="Distance" value={<b className="tnum">{estimate.distanceKm} km</b>} />
              <MoneyRow label="Vehicle" value={inr(estimate.vehicleCost)} />
              <MoneyRow label={`Driver (${plural(estimate.days, 'day')})`} value={inr(estimate.driverCost)} />
              <MoneyRow label={`GST (${estimate.gstPercent}%)`} value={inr(estimate.taxAmount)} />
              <MoneyRow label="Total" value={inr(estimate.totalAmount)} total />
            </MoneyRows>
          )}

          {save.error && (
            <Alert kind="error" className="mt-md">
              {save.error}
            </Alert>
          )}

          <Button
            type="submit"
            size="lg"
            block
            className="mt-lg"
            busy={save.busy}
            disabled={!estimate || estimate.overCapacity}
          >
            Continue to payment
          </Button>

          <p className="muted small mt-md">
            Tolls are charged at actuals. Your quote is held for 30 minutes.
          </p>
        </aside>
      </form>
    </>
  );
}

/** The starting route and vehicle shown when the page opens. */
function defaultChoices(meta: ReferenceData): Partial<BookingForm> {
  const cityId = (name: string, fallbackIndex: number) =>
    meta.cities.find((city) => city.name === name)?.id ?? meta.cities[fallbackIndex]?.id ?? 0;

  return {
    pickupCityId: cityId('Bengaluru', 0),
    dropCityId: cityId('Hubballi', 1),
    vehicleTypeId:
      meta.vehicleTypes.find((type) => type.code === '14FT')?.id ?? meta.vehicleTypes[0]?.id ?? 0,
    goodsCategoryId: meta.goods[0]?.id ?? 0,
  };
}
