/** Document types the portal asks for, and their display names. */

const DOCUMENT_NAMES: Record<string, string> = {
  RC: 'RC (registration)',
  Insurance: 'Insurance',
  Fitness: 'Fitness certificate',
  Permit: 'Permit',
  PUC: 'Pollution (PUC)',
  Aadhaar: 'Aadhaar',
  PAN: 'PAN card',
  CancelledCheque: 'Cancelled cheque',
  GST: 'GST certificate',
  Licence: 'Driving licence',
  DriverPhoto: 'Photo',
  Other: 'Other',
};

export function documentName(docType: string): string {
  return DOCUMENT_NAMES[docType] ?? docType;
}

/** Every vehicle needs these before it can take loads. */
export const VEHICLE_DOCUMENTS = ['RC', 'Insurance', 'Fitness', 'Permit', 'PUC'];

/** KYC documents each role must upload, then the optional extras. */
export const REQUIRED_KYC_DOCUMENTS: Record<string, string[]> = {
  Owner: ['Aadhaar', 'PAN', 'CancelledCheque'],
  Driver: ['Licence', 'Aadhaar', 'DriverPhoto'],
};

export const OPTIONAL_KYC_DOCUMENTS: Record<string, string[]> = {
  Owner: ['GST', 'Other'],
  Driver: ['Other'],
};

/** Documents that carry a "valid until" date. */
export const DOCUMENTS_WITH_EXPIRY = ['Insurance', 'Fitness', 'Permit', 'PUC', 'Licence'];
