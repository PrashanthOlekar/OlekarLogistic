/** Phone location for the trip log, and Google Maps directions links. */

export interface Position {
  latitude?: number;
  longitude?: number;
}

/** Best effort: if the phone refuses or is slow, the trip still moves on without a location. */
export function getCurrentPosition(): Promise<Position> {
  return new Promise((resolve) => {
    if (!navigator.geolocation) {
      resolve({});
      return;
    }
    navigator.geolocation.getCurrentPosition(
      (position) =>
        resolve({
          latitude: Number(position.coords.latitude.toFixed(6)),
          longitude: Number(position.coords.longitude.toFixed(6)),
        }),
      () => resolve({}),
      { timeout: 3000, maximumAge: 60_000 },
    );
  });
}

export function directionsUrl(address: string, city?: string | null): string {
  const destination = encodeURIComponent(`${address}, ${city ?? ''}`);
  return `https://www.google.com/maps/dir/?api=1&destination=${destination}`;
}
