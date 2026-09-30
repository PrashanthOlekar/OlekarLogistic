import { useEffect, useState, type FormEvent } from 'react';
import { api, getMeta, type Meta } from '../../api';
import { navigate } from '../../router';
import { Alert, Button, Field, PageHead, inr, kg, today, useAction } from '../../components/ui';

interface Estimate {
  distanceKm: number; days: number; vehicleCost: number; driverCost: number; freight: number; taxAmount: number; totalAmount: number; gstPercent: number;
  overCapacity: boolean; maxLoadKg: number; suggested?: { id: number; name: string } | null;
}

export function NewBookingPage() {
  const [meta, setMeta] = useState<Meta | null>(null);
  const [f, setF] = useState({
    pickupCityId: 0, pickupAddress: '', pickupContactName: '', pickupContactPhone: '',
    dropCityId: 0, dropAddress: '', dropContactName: '', dropContactPhone: '',
    goodsCategoryId: 0, goodsDescription: '', weightKg: 1000, goodsValue: '',
    vehicleTypeId: 0, pickupDate: today(), pickupSlot: 'Morning (6–10 am)', specialInstructions: ''
  });
  const [est, setEst] = useState<Estimate | null>(null);
  const [estErr, setEstErr] = useState<string | null>(null);
  const save = useAction();
  const set = (k: keyof typeof f, num = false) => (e: { target: { value: string } }) =>
    setF((s) => ({ ...s, [k]: num ? Number(e.target.value) : e.target.value }));

  useEffect(() => {
    getMeta().then((m) => {
      setMeta(m);
      const blr = m.cities.find((c) => c.name === 'Bengaluru')?.id ?? m.cities[0]?.id ?? 0;
      const hub = m.cities.find((c) => c.name === 'Hubballi')?.id ?? m.cities[1]?.id ?? 0;
      const vt = m.vehicleTypes.find((v) => v.code === '14FT')?.id ?? m.vehicleTypes[0]?.id ?? 0;
      setF((s) => ({ ...s, pickupCityId: blr, dropCityId: hub, vehicleTypeId: vt, goodsCategoryId: m.goods[0]?.id ?? 0 }));
    }).catch((e) => setEstErr(e.message));
  }, []);

  // Live price: re-quoted by the server whenever the route, vehicle or weight changes.
  useEffect(() => {
    if (!f.pickupCityId || !f.dropCityId || !f.vehicleTypeId || !f.weightKg) return;
    const t = setTimeout(() => {
      api<Estimate>('/quotes/estimate', { body: { pickupCityId: f.pickupCityId, dropCityId: f.dropCityId, vehicleTypeId: f.vehicleTypeId, weightKg: f.weightKg } })
        .then((r) => { setEst(r); setEstErr(null); }).catch((e) => setEstErr(e.message));
    }, 250);
    return () => clearTimeout(t);
  }, [f.pickupCityId, f.dropCityId, f.vehicleTypeId, f.weightKg]);

  const submit = (e: FormEvent) => {
    e.preventDefault();
    save.run(async () => {
      const r = await api<{ id: number }>('/bookings', { body: { ...f, goodsValue: f.goodsValue ? Number(f.goodsValue) : null } });
      navigate(`/customer/bookings/${r.id}`);
    });
  };

  const vt = meta?.vehicleTypes.find((v) => v.id === f.vehicleTypeId);
  const cityOpts = meta?.cities.map((c) => <option key={c.id} value={c.id}>{c.name}, {c.state}</option>);

  return (
    <>
      <PageHead title="Book a truck" sub="Tell us the route and the load. The price updates as you fill in the form." />
      <form className="grid split" onSubmit={submit}>
        <div className="card">
          <div className="form-section">
            <h3><span className="n">1</span>Pickup</h3>
            <div className="grid g2">
              <Field label="City"><select className="input" value={f.pickupCityId} onChange={set('pickupCityId', true)}>{cityOpts}</select></Field>
              <Field label="Date"><input className="input" type="date" min={today()} value={f.pickupDate} onChange={set('pickupDate')} /></Field>
            </div>
            <Field label="Address"><input className="input" value={f.pickupAddress} onChange={set('pickupAddress')} placeholder="Warehouse 12, Peenya Industrial Area" /></Field>
            <div className="grid g2">
              <Field label="Contact at pickup (optional)"><input className="input" value={f.pickupContactName} onChange={set('pickupContactName')} /></Field>
              <Field label="Their phone (optional)"><input className="input" inputMode="tel" value={f.pickupContactPhone} onChange={set('pickupContactPhone')} /></Field>
            </div>
            <Field label="Time slot">
              <select className="input" value={f.pickupSlot} onChange={set('pickupSlot')}>
                <option>Morning (6–10 am)</option><option>Midday (10 am–2 pm)</option><option>Afternoon (2–6 pm)</option><option>Night (after 8 pm)</option>
              </select>
            </Field>
          </div>
          <div className="form-section">
            <h3><span className="n">2</span>Delivery</h3>
            <Field label="City"><select className="input" value={f.dropCityId} onChange={set('dropCityId', true)}>{cityOpts}</select></Field>
            <Field label="Address"><input className="input" value={f.dropAddress} onChange={set('dropAddress')} placeholder="Gokul Road, Hubballi" /></Field>
            <div className="grid g2">
              <Field label="Receiver name (optional)"><input className="input" value={f.dropContactName} onChange={set('dropContactName')} /></Field>
              <Field label="Receiver phone (optional)"><input className="input" inputMode="tel" value={f.dropContactPhone} onChange={set('dropContactPhone')} /></Field>
            </div>
          </div>
          <div className="form-section">
            <h3><span className="n">3</span>Goods and vehicle</h3>
            <div className="grid g2">
              <Field label="Type of goods"><select className="input" value={f.goodsCategoryId} onChange={set('goodsCategoryId', true)}>{meta?.goods.map((g) => <option key={g.id} value={g.id}>{g.name}</option>)}</select></Field>
              <Field label="Approximate weight (kg)"><input className="input" type="number" min={1} value={f.weightKg} onChange={set('weightKg', true)} /></Field>
            </div>
            <Field label="What are you sending?"><input className="input" value={f.goodsDescription} onChange={set('goodsDescription')} placeholder="120 cartons of biscuits" /></Field>
            <Field label="Vehicle type" hint={vt ? `${vt.lengthFt} × ${vt.widthFt} ft · up to ${kg(vt.maxLoadKg)} · good for ${vt.recommendedGoods?.toLowerCase()}` : undefined}>
              <select className="input" value={f.vehicleTypeId} onChange={set('vehicleTypeId', true)}>
                {meta?.vehicleTypes.map((v) => <option key={v.id} value={v.id}>{v.name} · up to {kg(v.maxLoadKg)}</option>)}
              </select>
            </Field>
            <div className="grid g2">
              <Field label="Value of goods, ₹ (optional)" hint="Needed for an e-way bill above ₹50,000"><input className="input" type="number" min={0} value={f.goodsValue} onChange={set('goodsValue')} /></Field>
            </div>
            <Field label="Special instructions (optional)"><textarea className="input" value={f.specialInstructions} onChange={set('specialInstructions')} placeholder="Fragile, needs tarpaulin, unloading at 2nd floor…" /></Field>
          </div>
        </div>

        <aside className="card" style={{ position: 'sticky', top: 20 }}>
          <div className="card-head"><h2>Your price</h2>{est && <span className="pill green">Live</span>}</div>
          {estErr && <Alert kind="error">{estErr}</Alert>}
          {est?.overCapacity && (
            <Alert kind="warn" title={`Too heavy for this vehicle (max ${kg(est.maxLoadKg)})`}>
              {est.suggested && <Button size="sm" variant="secondary" onClick={() => setF((s) => ({ ...s, vehicleTypeId: est.suggested!.id }))} style={{ marginTop: 8 }}>Switch to {est.suggested.name}</Button>}
            </Alert>
          )}
          {est && (
            <div className="money-rows">
              <div><span>Distance</span><b className="tnum">{est.distanceKm} km</b></div>
              <div><span>Vehicle</span><span>{inr(est.vehicleCost)}</span></div>
              <div><span>Driver ({est.days} day{est.days > 1 ? 's' : ''})</span><span>{inr(est.driverCost)}</span></div>
              <div><span>GST ({est.gstPercent}%)</span><span>{inr(est.taxAmount)}</span></div>
              <div className="total"><span>Total</span><b>{inr(est.totalAmount)}</b></div>
            </div>
          )}
          {save.error && <div style={{ marginTop: 12 }}><Alert kind="error">{save.error}</Alert></div>}
          <Button type="submit" size="lg" block busy={save.busy} disabled={!est || est.overCapacity} style={{ marginTop: 16 }}>Continue to payment</Button>
          <p className="muted small" style={{ marginTop: 10 }}>Tolls are charged at actuals. Your quote is held for 30 minutes.</p>
        </aside>
      </form>
    </>
  );
}
