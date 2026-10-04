/** GET /reference-data: cities, vehicle types and goods categories. */
export interface ReferenceData {
  cities: City[];
  vehicleTypes: VehicleType[];
  goods: GoodsCategory[];
}

export interface City {
  id: number;
  name: string;
  nameKn?: string | null;
  state: string;
}

export interface VehicleType {
  id: number;
  code: string;
  name: string;
  bodyType: string;
  maxLoadKg: number;
  lengthFt: number;
  widthFt: number;
  heightFt?: number | null;
  recommendedGoods?: string | null;
}

export interface GoodsCategory {
  id: number;
  name: string;
}

/** POST /price-estimates */
export interface PriceEstimateRequest {
  pickupCityId: number;
  dropCityId: number;
  vehicleTypeId: number;
  weightKg: number;
}

export interface PriceEstimate {
  distanceKm: number;
  days: number;
  vehicleCost: number;
  driverCost: number;
  freight: number;
  taxAmount: number;
  totalAmount: number;
  gstPercent: number;
  overCapacity: boolean;
  maxLoadKg: number;
  suggested?: { id: number; name: string } | null;
}
