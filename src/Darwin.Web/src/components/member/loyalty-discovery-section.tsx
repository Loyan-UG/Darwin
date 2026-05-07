import Link from "next/link";
import { StatusBanner } from "@/components/feedback/status-banner";
import { LoyaltyDiscoveryMap } from "@/components/member/loyalty-discovery-map";
import type {
  BusinessCategoryKind,
  BusinessSummary,
} from "@/features/businesses/types";
import { buildLoyaltyBusinessPath } from "@/lib/entity-paths";
import { buildAppQueryPath, localizeHref } from "@/lib/locale-routing";
import { toWebApiUrl } from "@/lib/webapi-url";
import {
  formatResource,
  getMemberResource,
  resolveApiStatusLabel,
} from "@/localization";

type LoyaltyDiscoverySectionProps = {
  culture: string;
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
  return buildAppQueryPath("/loyalty", {
    discoverPage: input.page && input.page > 1 ? input.page : undefined,
    query: input.query,
    city: input.city,
    countryCode: input.countryCode,
    category: input.category,
    latitude: input.latitude,
    longitude: input.longitude,
    radiusKm: input.radiusKm,
  });
}

function formatDistance(
  distanceMeters: number | null | undefined,
  copy: ReturnType<typeof getMemberResource>,
) {
  if (!distanceMeters) {
    return null;
  }

  if (distanceMeters >= 1000) {
    return formatResource(copy.distanceKmAway, {
      value: (distanceMeters / 1000).toFixed(1),
    });
  }

  return formatResource(copy.distanceMAway, {
    value: Math.round(distanceMeters),
  });
}

export function LoyaltyDiscoverySection({
  culture,
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
  title,
  description,
}: LoyaltyDiscoverySectionProps) {
  const copy = getMemberResource(culture);
  const statusLabel = resolveApiStatusLabel(status, copy);

  return (
    <section className="rounded-[1rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-6 shadow-[var(--shadow-panel)]">
      <div className="flex flex-wrap items-start justify-between gap-4">
        <div>
          <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-brand)]">
            {copy.loyaltyDiscoveryEyebrow}
          </p>
          <h2 className="mt-3 text-2xl font-semibold text-[var(--color-text-primary)]">
            {title ?? copy.discoveryTitleDefault}
          </h2>
          <p className="mt-2 max-w-3xl text-sm leading-7 text-[var(--color-text-secondary)]">
            {description ?? copy.discoveryDescriptionDefault}
          </p>
        </div>
        <p className="rounded-full bg-[var(--color-surface-panel-strong)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)]">
          {formatResource(copy.visibleOnPageLabel, { count: businesses.length })}
        </p>
      </div>

      <div className="mt-5 rounded-[1rem] bg-[var(--color-surface-panel-strong)] px-4 py-4 text-sm leading-7 text-[var(--color-text-secondary)]">
        <p className="font-semibold text-[var(--color-text-primary)]">
          {copy.discoveryWindowTitle}
        </p>
        <p>
          {formatResource(copy.discoveryWindowMessage, {
            count: businesses.length,
            currentPage,
            totalPages,
            status: statusLabel ?? status,
          })}
        </p>
      </div>

      {status !== "ok" && (
        <div className="mt-5">
          <StatusBanner
            tone="warning"
            title={copy.discoveryWarningsTitle}
            message={formatResource(copy.discoveryWarningsMessage, {
              status: statusLabel ?? status,
            })}
          />
        </div>
      )}

      <form
        action={localizeHref("/loyalty", culture)}
        className="mt-5 grid gap-4 rounded-[1rem] bg-[var(--color-surface-panel-strong)] p-4 lg:grid-cols-6"
      >
        <label className="flex flex-col gap-2 text-sm font-semibold text-[var(--color-text-primary)]">
          {copy.searchLabel}
          <input
            type="text"
            name="query"
            defaultValue={query ?? ""}
            placeholder={copy.searchPlaceholder}
            className="rounded-2xl border border-[var(--color-border-soft)] bg-white/70 px-4 py-3 text-sm font-normal text-[var(--color-text-primary)] outline-none transition focus:border-[var(--color-brand)]"
          />
        </label>
        <label className="flex flex-col gap-2 text-sm font-semibold text-[var(--color-text-primary)]">
          {copy.cityLabel}
          <input
            type="text"
            name="city"
            defaultValue={city ?? ""}
            placeholder={copy.cityPlaceholder}
            className="rounded-2xl border border-[var(--color-border-soft)] bg-white/70 px-4 py-3 text-sm font-normal text-[var(--color-text-primary)] outline-none transition focus:border-[var(--color-brand)]"
          />
        </label>
        <label className="flex flex-col gap-2 text-sm font-semibold text-[var(--color-text-primary)]">
          {copy.countryLabel}
          <input
            type="text"
            name="countryCode"
            defaultValue={countryCode ?? ""}
            placeholder={copy.countryPlaceholder}
            className="rounded-2xl border border-[var(--color-border-soft)] bg-white/70 px-4 py-3 text-sm font-normal uppercase text-[var(--color-text-primary)] outline-none transition focus:border-[var(--color-brand)]"
          />
        </label>
        <label className="flex flex-col gap-2 text-sm font-semibold text-[var(--color-text-primary)]">
          {copy.categoryLabel}
          <select
            name="category"
            defaultValue={category ?? ""}
            className="rounded-2xl border border-[var(--color-border-soft)] bg-white/70 px-4 py-3 text-sm font-normal text-[var(--color-text-primary)] outline-none transition focus:border-[var(--color-brand)]"
          >
            <option value="">{copy.allLoyaltyReadyCategories}</option>
            {categoryKinds.map((item) => (
              <option key={item.key} value={item.key}>
                {item.displayName}
              </option>
            ))}
          </select>
        </label>
        <label className="flex flex-col gap-2 text-sm font-semibold text-[var(--color-text-primary)]">
          {copy.latitudeLabel}
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
          {copy.longitudeLabel}
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
          {copy.radiusKmLabel}
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
            {copy.applyFiltersCta}
          </button>
          <Link
            href={localizeHref("/loyalty", culture)}
            className="inline-flex rounded-full border border-[var(--color-border-soft)] px-5 py-3 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel)]"
          >
            {copy.resetCta}
          </Link>
        </div>
      </form>

      {(typeof latitude === "number" || typeof longitude === "number") && (
        <div className="mt-5 rounded-[1rem] bg-[var(--color-surface-panel-strong)] px-4 py-4 text-sm leading-7 text-[var(--color-text-secondary)]">
          <p className="font-semibold text-[var(--color-text-primary)]">
            {copy.proximityModeTitle}
          </p>
          <p>{copy.proximityModeMessage}</p>
        </div>
      )}

      <div className="mt-5">
        <LoyaltyDiscoveryMap
          culture={culture}
          businesses={businesses}
          latitude={latitude}
          longitude={longitude}
          radiusKm={radiusKm}
        />
      </div>

      {businesses.length > 0 ? (
        <div className="mt-5 grid gap-5 md:grid-cols-2 xl:grid-cols-3">
          {businesses.map((business) => (
            (() => {
              const logoUrl = toWebApiUrl(business.logoUrl ?? "");
              return (
            <article
              key={business.id}
              className="overflow-hidden rounded-[1rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel-strong)]"
            >
              <div className="flex min-h-44 items-center justify-center bg-[linear-gradient(145deg,rgba(228,240,212,0.95),rgba(255,253,248,1))] p-5">
                {logoUrl ? (
                  // eslint-disable-next-line @next/next/no-img-element
                  <img
                    src={logoUrl}
                    alt={business.name}
                    className="max-h-28 w-auto object-contain"
                  />
                ) : (
                  <div className="text-center">
                    <p className="text-xs font-semibold uppercase tracking-[0.2em] text-[var(--color-text-muted)]">
                      {business.category}
                    </p>
                    <p className="mt-2 text-sm text-[var(--color-text-secondary)]">
                      {copy.noBusinessMedia}
                    </p>
                  </div>
                )}
              </div>
              <div className="p-5">
                <div className="flex flex-wrap items-start justify-between gap-4">
                  <div>
                    <p className="text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-accent)]">
                      {business.isOpenNow === true
                        ? copy.openNow
                        : business.isOpenNow === false
                          ? copy.closedNow
                          : copy.loyaltyReady}
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
                  <p>
                    {business.shortDescription ??
                      copy.publicBusinessDetailCardFallback}
                  </p>
                  {business.city ? <p className="mt-2">{business.city}</p> : null}
                  {typeof business.rating === "number" ? (
                    <p>
                      {formatResource(copy.ratingLabel, {
                        rating: business.rating.toFixed(1),
                        reviews: business.ratingCount
                          ? formatResource(copy.reviewsSuffix, {
                              count: business.ratingCount,
                            })
                          : "",
                      })}
                    </p>
                  ) : null}
                  {formatDistance(business.distanceMeters, copy) ? (
                    <p>{formatDistance(business.distanceMeters, copy)}</p>
                  ) : null}
                </div>

                <div className="mt-5">
                  <Link
                          href={localizeHref(buildLoyaltyBusinessPath(business.id), culture)}
                    className="inline-flex rounded-full border border-[var(--color-border-soft)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel)]"
                  >
                    {copy.openLoyaltyPlaceCta}
                  </Link>
                </div>
              </div>
            </article>
              );
            })()
          ))}
        </div>
      ) : (
        <div className="mt-5 rounded-[1rem] border border-dashed border-[var(--color-border-strong)] px-5 py-8 text-center">
          <p className="text-sm leading-7 text-[var(--color-text-secondary)]">
            {copy.noDiscoveryMatchesMessage}
          </p>
          <div className="mt-5 flex flex-wrap justify-center gap-3">
            <Link
              href={localizeHref("/catalog", culture)}
              className="inline-flex rounded-full border border-[var(--color-border-soft)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel)]"
            >
              {copy.memberCrossSurfaceCatalogCta}
            </Link>
            <Link
              href={localizeHref("/", culture)}
              className="inline-flex rounded-full border border-[var(--color-border-soft)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel)]"
            >
              {copy.memberCrossSurfaceHomeCta}
            </Link>
          </div>
        </div>
      )}

      {totalPages > 1 && (
        <div className="mt-5 flex flex-wrap items-center gap-3">
          <Link
            aria-disabled={currentPage <= 1}
            href={localizeHref(
              buildDiscoveryHref({
                page: Math.max(1, currentPage - 1),
                query,
                city,
                countryCode,
                category,
                latitude,
                longitude,
                radiusKm,
              }),
              culture,
            )}
            className="rounded-full border border-[var(--color-border-soft)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel-strong)] aria-[disabled=true]:pointer-events-none aria-[disabled=true]:opacity-40"
          >
            {copy.previous}
          </Link>
          <p className="text-sm text-[var(--color-text-secondary)]">
            {formatResource(copy.discoveryPageLabel, { currentPage, totalPages })}
          </p>
          <Link
            aria-disabled={currentPage >= totalPages}
            href={localizeHref(
              buildDiscoveryHref({
                page: Math.min(totalPages, currentPage + 1),
                query,
                city,
                countryCode,
                category,
                latitude,
                longitude,
                radiusKm,
              }),
              culture,
            )}
            className="rounded-full border border-[var(--color-border-soft)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel-strong)] aria-[disabled=true]:pointer-events-none aria-[disabled=true]:opacity-40"
          >
            {copy.next}
          </Link>
        </div>
      )}
    </section>
  );
}
