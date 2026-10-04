/** One module per API area. Pages import these instead of calling Axios directly. */
export { approvalsApi, dashboardsApi, paymentsApi, usersApi } from './adminApi';
export { ApiError } from './errors';
export { authApi } from './authApi';
export { bookingsApi } from './bookingsApi';
export { documentsApi } from './documentsApi';
export { driversApi, ownersApi } from './driversApi';
export { loadsApi } from './loadsApi';
export { referenceDataApi } from './referenceDataApi';
export { registrationsApi } from './registrationsApi';
export { settlementsApi } from './settlementsApi';
export { tripsApi } from './tripsApi';
export { vehiclesApi } from './vehiclesApi';
