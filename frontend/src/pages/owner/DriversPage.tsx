import { EmptyCard, Loading, PageHead, Pill } from '../../components';
import { useLoad } from '../../hooks/useLoad';
import { api } from '../../lib/api';
import { date } from '../../lib/format';
import type { OwnerDriver } from './types';

export function DriversPage() {
  const drivers = useLoad(() => api<OwnerDriver[]>('/owner/drivers'));

  return (
    <>
      <PageHead
        title="Drivers"
        sub="Drivers link themselves to you by entering your mobile number when they register."
      />

      <Loading state={drivers} />

      {drivers.data?.length === 0 && (
        <EmptyCard title="No drivers linked yet">
          Ask your driver to open the ProCargo portal, choose “I drive”, and enter your mobile number as their
          owner.
        </EmptyCard>
      )}

      {!!drivers.data?.length && (
        <div className="card table-wrap">
          <table>
            <thead>
              <tr>
                <th>Driver</th>
                <th>Mobile</th>
                <th>Licence</th>
                <th>Valid until</th>
                <th>KYC</th>
                <th>Duty</th>
              </tr>
            </thead>
            <tbody>
              {drivers.data.map((driver) => (
                <tr key={driver.id}>
                  <td>
                    <b>{driver.name}</b>
                  </td>
                  <td className="mono">{driver.mobile}</td>
                  <td className="mono">
                    {driver.licenceNumber} <span className="muted">({driver.licenceClass})</span>
                  </td>
                  <td>{date(driver.licenceExpiry)}</td>
                  <td>
                    <Pill status={driver.kycStatus} />
                  </td>
                  <td>
                    <Pill status={driver.dutyStatus} />
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </>
  );
}
