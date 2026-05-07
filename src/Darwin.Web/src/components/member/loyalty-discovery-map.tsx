import Link from "next/link";
import type { BusinessSummary } from "@/features/businesses/types";
import { buildLoyaltyBusinessPath } from "@/lib/entity-paths";
import { formatResource, getMemberResource } from "@/localization";
import { localizeHref } from "@/lib/locale-routing";

type LoyaltyDiscoveryMapProps = {
  culture: string;
  businesses: BusinessSummary[];
  latitude?: number;
  longitude?: number;
  radiusKm?: number;
};

type PositionedBusiness = {
  business: BusinessSummary;
  left: number;
  top: number;
};

function toPositionedBusinesses(
  businesses: BusinessSummary[],
): PositionedBusiness[] {
  const withCoordinates = businesses.filter(
    (business): business is BusinessSummary & {
      location: NonNullable<BusinessSummary["location"]>;
    } => Boolean(business.location),
  );

  if (withCoordinates.length === 0) {
    return [];
  }

  const north = Math.max(
    ...withCoordinates.map((business) => business.location.latitude),
  );
  const south = Math.min(
    ...withCoordinates.map((business) => business.location.latitude),
  );
  const east = Math.max(
    ...withCoordinates.map((business) => business.location.longitude),
  );
  const west = Math.min(
    ...withCoordinates.map((business) => business.location.longitude),
  );
  const latSpan = Math.max(0.02, north - south);
  const lonSpan = Math.max(0.02, east - west);

  return withCoordinates.map((business) => ({
    business,
    left: ((business.location.longitude - west) / lonSpan) * 100,
    top: ((north - business.location.latitude) / latSpan) * 100,
  }));
}

export function LoyaltyDiscoveryMap({
  culture,
  businesses,
  latitude,
  longitude,
  radiusKm,
}: LoyaltyDiscoveryMapProps) {
  const copy = getMemberResource(culture);
  const positionedBusinesses = toPositionedBusinesses(businesses);

  if (positionedBusinesses.length === 0) {
    return (
      <div className="rounded-[1rem] border border-dashed border-[var(--color-border-strong)] px-5 py-8 text-sm leading-7 text-[var(--color-text-secondary)]">
        {copy.mapPreviewUnavailableMessage}
      </div>
    );
  }

  return (
    <div className="grid gap-4 lg:grid-cols-[minmax(0,1.1fr)_300px]">
      <div className="relative min-h-[22rem] overflow-hidden rounded-[1rem] border border-[var(--color-border-soft)] bg-[radial-gradient(circle_at_top,rgba(228,240,212,0.95),rgba(255,253,248,1))]">
        <div className="absolute inset-0 bg-[linear-gradient(to_right,rgba(168,184,153,0.18)_1px,transparent_1px),linear-gradient(to_bottom,rgba(168,184,153,0.18)_1px,transparent_1px)] bg-[size:24px_24px]" />
        {typeof latitude === "number" && typeof longitude === "number" ? (
          <div className="absolute left-1/2 top-1/2 z-10 -translate-x-1/2 -translate-y-1/2">
            <div className="flex h-5 w-5 items-center justify-center rounded-full border-2 border-white bg-[var(--color-brand)] shadow-[0_0_0_8px_rgba(211,134,73,0.18)]" />
          </div>
        ) : null}
        {positionedBusinesses.map(({ business, left, top }) => (
          <Link
            key={business.id}
                href={localizeHref(buildLoyaltyBusinessPath(business.id), culture)}
            className="absolute z-20 flex -translate-x-1/2 -translate-y-1/2 flex-col items-center gap-2"
            style={{
              left: `${Math.max(10, Math.min(90, left))}%`,
              top: `${Math.max(10, Math.min(90, top))}%`,
            }}
          >
            <span className="rounded-full border border-white bg-[var(--color-accent)] px-2 py-1 text-[10px] font-semibold uppercase tracking-[0.12em] text-white shadow-lg">
              {business.category}
            </span>
            <span className="flex h-4 w-4 rounded-full border-2 border-white bg-[var(--color-text-primary)] shadow-lg" />
          </Link>
        ))}
        <div className="absolute bottom-4 left-4 rounded-full bg-white/85 px-4 py-2 text-xs font-semibold uppercase tracking-[0.14em] text-[var(--color-text-primary)] shadow-sm">
          {typeof latitude === "number" && typeof longitude === "number"
            ? `${copy.proximityLensLabel}${radiusKm ? ` | ${radiusKm} km` : ""}`
            : copy.coordinatePreviewLabel}
        </div>
      </div>

      <div className="rounded-[1rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel-strong)] px-5 py-5">
        <p className="text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-accent)]">
          {copy.mapLegendTitle}
        </p>
        <div className="mt-4 flex flex-col gap-3">
          {positionedBusinesses.slice(0, 6).map(({ business }) => (
            <Link
              key={business.id}
              href={localizeHref(buildLoyaltyBusinessPath(business.id), culture)}
              className="rounded-[1rem] bg-white/80 px-4 py-3 text-sm leading-6 text-[var(--color-text-secondary)] transition hover:bg-white"
            >
              <p className="font-semibold text-[var(--color-text-primary)]">
                {business.name}
              </p>
              <p>{business.city ?? business.category}</p>
              {business.distanceMeters ? (
                <p>
                  {formatResource(copy.distanceKmAway, {
                    value: (business.distanceMeters / 1000).toFixed(1),
                  })}
                </p>
              ) : null}
            </Link>
          ))}
        </div>
      </div>
    </div>
  );
}
