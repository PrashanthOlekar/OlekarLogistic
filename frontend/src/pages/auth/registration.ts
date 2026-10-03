/** Account types offered on the registration page, and the request body each one sends. */

export type AccountKind = 'customer' | 'owner' | 'driver';

export const ACCOUNT_KINDS: { id: AccountKind; label: string; sub: string }[] = [
  { id: 'customer', label: 'I need transport', sub: 'Book lorries for your goods' },
  { id: 'owner', label: 'I own lorries', sub: 'Get loads for your vehicles' },
  { id: 'driver', label: 'I drive', sub: 'Run trips for an owner' },
];

/** Where each new account lands first. */
export const FIRST_PAGE: Record<AccountKind, string> = {
  customer: '/customer/book',
  owner: '/owner/documents',
  driver: '/driver/documents',
};

/** All registration inputs, keyed by field name. Empty optional fields are sent as null. */
export type RegistrationForm = Record<string, string>;

export function buildRegistrationBody(
  kind: AccountKind,
  form: RegistrationForm,
  mobile: string,
  code: string,
) {
  const optional = (key: string) => form[key] || null;
  const required = (key: string) => form[key] ?? '';

  const common = { fullName: required('fullName'), mobile, code, email: optional('email') };

  switch (kind) {
    case 'customer':
      return { ...common, companyName: optional('companyName'), gstin: optional('gstin') };

    case 'owner':
      return {
        ...common,
        businessName: optional('businessName'),
        pan: required('pan'),
        aadhaarLast4: required('aadhaarLast4'),
        accountHolder: required('accountHolder'),
        accountNumber: required('accountNumber'),
        ifsc: required('ifsc'),
        bankName: optional('bankName'),
      };

    case 'driver':
      return {
        ...common,
        licenceNumber: required('licenceNumber'),
        licenceClass: form.licenceClass,
        licenceExpiry: required('licenceExpiry'),
        aadhaarLast4: optional('aadhaarLast4'),
        emergencyContactName: optional('emergencyContactName'),
        emergencyContactPhone: optional('emergencyContactPhone'),
        ownerMobile: optional('ownerMobile'),
      };
  }
}
