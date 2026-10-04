import { driversApi } from '../../api/driversApi';
import { EmptyCard, Loading, PageHead, Pagination, Pill } from '../../components';
import { usePagedLoad } from '../../hooks/usePagedLoad';
import { date } from '../../utils/format';

export function DriversPage() {
  const drivers = usePagedLoad((page) => driversApi.list(page));

  return (
    <>
      <PageHead
        title="Drivers"
        sub="Drivers link themselves to you by entering your mobile number when they register."
      />

      <Loading state={drivers} />

      {drivers.items?.length === 0 && (
        <EmptyCard title="No drivers linked yet">
          Ask your driver to open the ProCargo portal, choose “I drive”, and enter your mobile number as their
          owner.
        </EmptyCard>
      )}

      {!!drivers.items?.length && (
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
              {drivers.items.map((driver) => (
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
          <Pagination page={drivers.data} onPageChange={drivers.setPageNumber} />
        </div>
      )}
    </>
  );
}
