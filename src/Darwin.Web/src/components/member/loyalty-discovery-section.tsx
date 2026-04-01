import Link from "next/link";
import { StatusBanner } from "@/components/feedback/status-banner";
import { LoyaltyDiscoveryMap } from "@/components/member/loyalty-discovery-map";
import type {
  BusinessCategoryKind,
  BusinessSummary,
} from "@/features/businesses/types";

type LoyaltyDiscoverySectionProps = {
  businesses: BusinessSummary[];
  status: string;
  currentPage: number;
  totalPages: number;
  query?: string;
  city?: string;
  countryCode?: string;
  category?: string;
  latitude?: number;
  longitude?: number;
  radiusKm?: number;
  categoryKinds: BusinessCategoryKind[];
  title?: string;
  description?: string;
};

function buildDiscoveryHref(input: {
  page?: number;
  query?: string;
  city?: string;
  countryCode?: string;
  category?: string;
  latitude?: number;
  longitude?: number;
  radiusKm?: number;
}) {
  const params = new URLSearchParams();

  if (input.page && input.page > 1) {
    params.set("discoverPage", String(input.page));
  }

  if (input.query) {
    params.set("query", input.query);
  }

  if (input.city) {
    params.set("city", input.city);
  }

  if (input.countryCode) {
    params.set("countryCode", input.countryCode);
  }

  if (input.category) {
    params.set("category", input.category);
  }

  if (typeof input.latitude === "number") {
    params.set("latitude", String(input.latitude));
  }

  if (typeof input.longitude === "number") {
    params.set("longitude", String(input.longitude));
  }

  if (typeof input.radiusKm === "number") {
    params.set("radiusKm", String(input.radiusKm));
  }

  const serialized = params.toString();
  return serialized ? `/loyalty?${serialized}` : "/loyalty";
}

function formatDistance(distanceMeters?: number | null) {
  if (!distanceMeters) {
    return null;
  }

  if (distanceMeters >= 1000) {
    return `${(distanceMeters / 1000).toFixed(1)} km away`;
  }

  return `${distanceMeters} m away`;
}

export function LoyaltyDiscoverySection({
  businesses,
  status,
  currentPage,
  totalPages,
  query,
  city,
  countryCode,
  category,
  latitude,
  longitude,
  radiusKm,
  categoryKinds,
  title = "Discover loyalty places",
  description = "Public discovery now uses the canonical business-discovery contracts and keeps loyalty-ready businesses visible even before the member joins.",
}: LoyaltyDiscoverySectionProps) {
  return (
    <section className="rounded-[2rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-6 shadow-[var(--shadow-panel)]">
      <div className="flex flex-wrap items-start justify-between gap-4">
        <div>
          <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-brand)]">
            Loyalty discovery
          </p>
          <h2 className="mt-3 text-2xl font-semibold text-[var(--color-text-primary)]">
            {title}
          </h2>
          <p className="mt-2 max-w-3xl text-sm leading-7 text-[var(--color-text-secondary)]">
            {description}
          </p>
        </div>
        <p className="rounded-full bg-[var(--color-surface-panel-strong)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)]">
          {businesses.length} visible on this page
        </p>
      </div>

      {status !== "ok" && (
        <div className="mt-5">
          <StatusBanner
            tone="warning"
            title="Business discovery loaded with warnings."
            message={`Discovery status: ${status}. Public loyalty discovery should remain observable instead of silently disappearing.`}
          />
        </div>
      )}

      <form action="/loyalty" className="mt-5 grid gap-4 rounded-[1.5rem] bg-[var(--color-surface-panel-strong)] p-4 lg:grid-cols-6">
        <label className="flex flex-col gap-2 text-sm font-semibold text-[var(--color-text-primary)]">
          Search
          <input
            type="text"
            name="query"
            defaultValue={query ?? ""}
            placeholder="Coffee, bakery, florist..."
            className="rounded-2xl border border-[var(--color-border-soft)] bg-white/70 px-4 py-3 text-sm font-normal text-[var(--color-text-primary)] outline-none transition focus:border-[var(--color-brand)]"
          />
        </label>
        <label className="flex flex-col gap-2 text-sm font-semibold text-[var(--color-text-primary)]">
          City
          <input
            type="text"
            name="city"
            defaultValue={city ?? ""}
            placeholder="Berlin"
            className="rounded-2xl border border-[var(--color-border-soft)] bg-white/70 px-4 py-3 text-sm font-normal text-[var(--color-text-primary)] outline-none transition focus:border-[var(--color-brand)]"
          />
        </label>
        <label className="flex flex-col gap-2 text-sm font-semibold text-[var(--color-text-primary)]">
          Country
          <input
            type="text"
            name="countryCode"
            defaultValue={countryCode ?? ""}
            placeholder="DE"
            className="rounded-2xl border border-[var(--color-border-soft)] bg-white/70 px-4 py-3 text-sm font-normal uppercase text-[var(--color-text-primary)] outline-none transition focus:border-[var(--color-brand)]"
          />
        </label>
        <label className="flex flex-col gap-2 text-sm font-semibold text-[var(--color-text-primary)]">
          Category
          <select
            name="category"
            defaultValue={category ?? ""}
            className="rounded-2xl border border-[var(--color-border-soft)] bg-white/70 px-4 py-3 text-sm font-normal text-[var(--color-text-primary)] outline-none transition focus:border-[var(--color-brand)]"
          >
            <option value="">All loyalty-ready categories</option>
            {categoryKinds.map((item) => (
              <option key={item.key} value={item.key}>
                {item.displayName}
              </option>
            ))}
          </select>
        </label>
        <label className="flex flex-col gap-2 text-sm font-semibold text-[var(--color-text-primary)]">
          Latitude
          <input
            type="number"
            step="0.0001"
            name="latitude"
            defaultValue={typeof latitude === "number" ? latitude : ""}
            placeholder="52.5200"
            className="rounded-2xl border border-[var(--color-border-soft)] bg-white/70 px-4 py-3 text-sm font-normal text-[var(--color-text-primary)] outline-none transition focus:border-[var(--color-brand)]"
          />
        </label>
        <label className="flex flex-col gap-2 text-sm font-semibold text-[var(--color-text-primary)]">
          Longitude
          <input
            type="number"
            step="0.0001"
            name="longitude"
            defaultValue={typeof longitude === "number" ? longitude : ""}
            placeholder="13.4050"
            className="rounded-2xl border border-[var(--color-border-soft)] bg-white/70 px-4 py-3 text-sm font-normal text-[var(--color-text-primary)] outline-none transition focus:border-[var(--color-brand)]"
          />
        </label>
        <label className="flex flex-col gap-2 text-sm font-semibold text-[var(--color-text-primary)]">
          Radius km
          <input
            type="number"
            min="1"
            max="50"
            step="1"
            name="radiusKm"
            defaultValue={typeof radiusKm === "number" ? radiusKm : ""}
            placeholder="8"
            className="rounded-2xl border border-[var(--color-border-soft)] bg-white/70 px-4 py-3 text-sm font-normal text-[var(--color-text-primary)] outline-none transition focus:border-[var(--color-brand)]"
          />
        </label>
        <div className="flex items-end gap-3">
          <button
            type="submit"
            className="inline-flex rounded-full bg-[var(--color-brand)] px-5 py-3 text-sm font-semibold text-[var(--color-brand-contrast)] transition hover:bg-[var(--color-brand-strong)]"
          >
            Apply filters
          </button>
          <Link
            href="/loyalty"
            className="inline-flex rounded-full border border-[var(--color-border-soft)] px-5 py-3 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel)]"
          >
            Reset
          </Link>
        </div>
      </form>

      {(typeof latitude === "number" || typeof longitude === "number") && (
        <div className="mt-5 rounded-[1.5rem] bg-[var(--color-surface-panel-strong)] px-4 py-4 text-sm leading-7 text-[var(--color-text-secondary)]">
          <p className="font-semibold text-[var(--color-text-primary)]">
            Proximity mode is active.
          </p>
          <p>
            Discovery list ranking now uses the current latitude/longitude and
            radius when they are valid. The visual map preview below is rendered
            from the same discovery result set so loyalty-only filtering stays
            consistent.
          </p>
        </div>
      )}

      <div className="mt-5">
        <LoyaltyDiscoveryMap
          businesses={businesses}
          latitude={latitude}
          longitude={longitude}
          radiusKm={radiusKm}
        />
      </div>

      {businesses.length > 0 ? (
        <div className="mt-5 grid gap-5 md:grid-cols-2 xl:grid-cols-3">
          {businesses.map((business) => (
            <article
              key={business.id}
              className="overflow-hidden rounded-[1.5rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel-strong)]"
            >
              <div className="flex min-h-44 items-center justify-center bg-[linear-gradient(145deg,rgba(228,240,212,0.95),rgba(255,253,248,1))] p-5">
                {business.logoUrl ? (
                  // eslint-disable-next-line @next/next/no-img-element
                  <img
                    src={business.logoUrl}
                    alt={business.name}
                    className="max-h-28 w-auto object-contain"
                  />
                ) : (
                  <div className="text-center">
                    <p className="text-xs font-semibold uppercase tracking-[0.2em] text-[var(--color-text-muted)]">
                      {business.category}
                    </p>
                    <p className="mt-2 text-sm text-[var(--color-text-secondary)]">
                      No business media
                    </p>
                  </div>
                )}
              </div>
              <div className="p-5">
                <div className="flex flex-wrap items-start justify-between gap-4">
                  <div>
                    <p className="text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-accent)]">
                      {business.isOpenNow === true
                        ? "Open now"
                        : business.isOpenNow === false
                          ? "Closed now"
                          : "Loyalty-ready"}
                    </p>
                    <h3 className="mt-3 text-2xl font-semibold text-[var(--color-text-primary)]">
                      {business.name}
                    </h3>
                  </div>
                  <span className="rounded-full bg-[var(--color-surface-panel)] px-3 py-2 text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-text-primary)]">
                    {business.category}
                  </span>
                </div>

                <div className="mt-4 text-sm leading-7 text-[var(--color-text-secondary)]">
                  <p>{business.shortDescription ?? "Public business detail is available for this loyalty place."}</p>
                  {business.city ? <p className="mt-2">{business.city}</p> : null}
                  {typeof business.rating === "number" ? (
                    <p>
                      Rating {business.rating.toFixed(1)}
                      {business.ratingCount ? ` from ${business.ratingCount} reviews` : ""}
                    </p>
                  ) : null}
                  {formatDistance(business.distanceMeters) ? (
                    <p>{formatDistance(business.distanceMeters)}</p>
                  ) : null}
                </div>

                <div className="mt-5">
                  <Link
                    href={`/loyalty/${business.id}`}
                    className="inline-flex rounded-full border border-[var(--color-border-soft)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel)]"
                  >
                    Open loyalty place
                  </Link>
                </div>
              </div>
            </article>
          ))}
        </div>
      ) : (
        <div className="mt-5 rounded-[1.5rem] border border-dashed border-[var(--color-border-strong)] px-5 py-8 text-sm leading-7 text-[var(--color-text-secondary)]">
          No loyalty-ready businesses matched the current filters.
        </div>
      )}

      {totalPages > 1 && (
        <div className="mt-5 flex flex-wrap items-center gap-3">
          <Link
            aria-disabled={currentPage <= 1}
            href={buildDiscoveryHref({
              page: Math.max(1, currentPage - 1),
              query,
              city,
              countryCode,
              category,
              latitude,
              longitude,
              radiusKm,
            })}
            className="rounded-full border border-[var(--color-border-soft)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel-strong)] aria-[disabled=true]:pointer-events-none aria-[disabled=true]:opacity-40"
          >
            Previous
          </Link>
          <p className="text-sm text-[var(--color-text-secondary)]">
            Discovery page {currentPage} of {totalPages}
          </p>
          <Link
            aria-disabled={currentPage >= totalPages}
            href={buildDiscoveryHref({
              page: Math.min(totalPages, currentPage + 1),
              query,
              city,
              countryCode,
              category,
              latitude,
              longitude,
              radiusKm,
            })}
            className="rounded-full border border-[var(--color-border-soft)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel-strong)] aria-[disabled=true]:pointer-events-none aria-[disabled=true]:opacity-40"
          >
            Next
          </Link>
        </div>
      )}
    </section>
  );
}
