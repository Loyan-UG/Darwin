import Link from "next/link";
import { StatusBanner } from "@/components/feedback/status-banner";
import type { PublicCategorySummary } from "@/features/catalog/types";
import type { PublicPageSummary } from "@/features/cms/types";
import { joinMemberLoyaltyBusinessAction } from "@/features/member-portal/actions";
import type {
  BusinessDetail,
  BusinessLocation,
  LoyaltyRewardTierPublic,
} from "@/features/businesses/types";
import { buildCmsPagePath, buildLoyaltyBusinessPath } from "@/lib/entity-paths";
import { buildAppQueryPath, buildLocalizedAuthHref, localizeHref } from "@/lib/locale-routing";
import {
  getSafeExternalLinkProps,
  toSafeHttpUrl,
  toWebApiUrl,
} from "@/lib/webapi-url";
import {
  formatResource,
  getMemberResource,
  matchesLocalizedQueryMessageKey,
  resolveApiStatusLabel,
  resolveLocalizedQueryMessage,
} from "@/localization";

type LoyaltyPublicBusinessPageProps = {
  culture: string;
  businessId: string;
  detail: BusinessDetail | null;
  detailStatus: string;
  isAuthenticated: boolean;
  joinStatus?: string;
  joinError?: string;
  cmsPages: PublicPageSummary[];
  cmsPagesStatus: string;
  categories: PublicCategorySummary[];
  categoriesStatus: string;
};

function formatLocation(location: BusinessLocation) {
  return [
    location.addressLine1,
    location.addressLine2,
    location.postalCode,
    location.city,
    location.region,
    location.countryCode,
  ]
    .filter(Boolean)
    .join(", ");
}

function formatRewardValue(reward: LoyaltyRewardTierPublic) {
  if (reward.rewardValue === undefined || reward.rewardValue === null) {
    return reward.rewardType;
  }

  return `${reward.rewardType} ${reward.rewardValue}`;
}

export function LoyaltyPublicBusinessPage({
  culture,
  businessId,
  detail,
  detailStatus,
  isAuthenticated,
  joinStatus,
  joinError,
  cmsPages,
  cmsPagesStatus,
  categories,
  categoriesStatus,
}: LoyaltyPublicBusinessPageProps) {
  const copy = getMemberResource(culture);
  const detailStatusLabel = resolveApiStatusLabel(detailStatus, copy);
  const cmsPagesStatusLabel = resolveApiStatusLabel(cmsPagesStatus, copy);
  const categoriesStatusLabel = resolveApiStatusLabel(categoriesStatus, copy);
  const resolvedJoinError = resolveLocalizedQueryMessage(joinError, copy);
  const locations = detail?.locations ?? [];
  const rewardTiers = detail?.loyaltyProgramPublic?.rewardTiers ?? [];
  const heroImage = toWebApiUrl(
    detail?.primaryImageUrl ?? detail?.galleryImageUrls?.[0] ?? detail?.imageUrls?.[0] ?? "",
  );
  const websiteUrl = toSafeHttpUrl(detail?.websiteUrl ?? "");
  const safeExternalLinkProps = getSafeExternalLinkProps();

  return (
    <section className="mx-auto flex w-full max-w-[var(--content-max-width)] flex-1 px-5 py-12 sm:px-6 lg:px-8">
      <div className="flex w-full flex-col gap-8">
        <div className="overflow-hidden rounded-[1rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] shadow-[var(--shadow-panel)]">
          <div className="grid gap-0 lg:grid-cols-[minmax(0,1.15fr)_minmax(320px,0.85fr)]">
            <div className="px-6 py-8 sm:px-8 sm:py-10">
              <p className="text-xs font-semibold uppercase tracking-[0.26em] text-[var(--color-brand)]">
                {copy.loyaltyPlaceEyebrow}
              </p>
              <h1 className="mt-4 font-[family-name:var(--font-display)] text-4xl leading-tight text-[var(--color-text-primary)] sm:text-5xl">
                {detail?.name ?? copy.loyaltyBusinessFallback}
              </h1>
              <div className="mt-5 flex flex-wrap gap-3 text-sm font-semibold text-[var(--color-text-secondary)]">
                {detail?.category ? (
                  <span className="rounded-full bg-[var(--color-surface-panel-strong)] px-4 py-2">
                    {detail.category}
                  </span>
                ) : null}
                {detail?.city ? (
                  <span className="rounded-full bg-[var(--color-surface-panel-strong)] px-4 py-2">
                    {detail.city}
                  </span>
                ) : null}
                {detail?.loyaltyProgramPublic?.isActive ? (
                  <span className="rounded-full bg-[var(--color-surface-panel-strong)] px-4 py-2">
                    {copy.loyaltyActiveLabel}
                  </span>
                ) : null}
              </div>
              <p className="mt-5 max-w-3xl text-base leading-8 text-[var(--color-text-secondary)] sm:text-lg">
                {detail?.shortDescription ??
                  detail?.description ??
                  copy.publicBusinessDetailFallback}
              </p>

              <div className="mt-8 flex flex-wrap gap-3">
                {isAuthenticated ? (
                  <form action={joinMemberLoyaltyBusinessAction}>
                    <input type="hidden" name="businessId" value={businessId} />
              <input type="hidden" name="returnPath" value={localizeHref(buildLoyaltyBusinessPath(businessId), culture)} />
                    {locations.length > 0 ? (
                      <select
                        name="businessLocationId"
                        defaultValue=""
                        className="mb-3 block w-full rounded-2xl border border-[var(--color-border-soft)] bg-white/70 px-4 py-3 text-sm text-[var(--color-text-primary)] outline-none transition focus:border-[var(--color-brand)]"
                      >
                        <option value="">{copy.joinWithoutBranchSelection}</option>
                        {locations.map((location) => (
                          <option
                            key={location.businessLocationId}
                            value={location.businessLocationId}
                          >
                            {location.name}
                            {formatLocation(location)
                              ? ` - ${formatLocation(location)}`
                              : ""}
                          </option>
                        ))}
                      </select>
                    ) : null}
                    <button
                      type="submit"
                      className="inline-flex rounded-full bg-[var(--color-brand)] px-5 py-3 text-sm font-semibold text-[var(--color-brand-contrast)] transition hover:bg-[var(--color-brand-strong)]"
                    >
                      {copy.joinLoyaltyCta}
                    </button>
                  </form>
                ) : (
                  <div className="flex flex-wrap gap-3">
                    <Link
                      href={buildLocalizedAuthHref(
                        "/account/sign-in",
              buildLoyaltyBusinessPath(businessId),
                        culture,
                      )}
                      className="inline-flex rounded-full bg-[var(--color-brand)] px-5 py-3 text-sm font-semibold text-[var(--color-brand-contrast)] transition hover:bg-[var(--color-brand-strong)]"
                    >
                      {copy.signInToJoinCta}
                    </Link>
                    <Link
                      href={buildLocalizedAuthHref(
                        "/account/register",
              buildLoyaltyBusinessPath(businessId),
                        culture,
                      )}
                      className="inline-flex rounded-full border border-[var(--color-border-soft)] px-5 py-3 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel-strong)]"
                    >
                      {copy.loyaltyCreateAccountCta}
                    </Link>
                  </div>
                )}
                <Link
                  href={localizeHref("/loyalty", culture)}
                  className="inline-flex rounded-full border border-[var(--color-border-soft)] px-5 py-3 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel-strong)]"
                >
                  {copy.backToLoyaltyCta}
                </Link>
              </div>
            </div>

            <div className="flex min-h-72 items-center justify-center bg-[linear-gradient(145deg,rgba(228,240,212,0.95),rgba(255,253,248,1))] p-8">
              {heroImage ? (
                // eslint-disable-next-line @next/next/no-img-element
                <img
                  src={heroImage}
                  alt={detail?.name ?? copy.loyaltyBusinessFallback}
                  className="max-h-72 w-auto object-contain"
                />
              ) : (
                <div className="text-center">
                  <p className="text-xs font-semibold uppercase tracking-[0.2em] text-[var(--color-text-muted)]">
                    {detail?.category ?? copy.loyaltyBusinessFallback}
                  </p>
                  <p className="mt-2 text-sm text-[var(--color-text-secondary)]">
                    {copy.noBusinessMedia}
                  </p>
                </div>
              )}
            </div>
          </div>
        </div>

        {(detailStatus !== "ok" || resolvedJoinError || joinStatus === "joined") && (
          <div className="flex flex-col gap-3">
            {detailStatus !== "ok" && (
              <StatusBanner
                tone="warning"
                title={copy.businessDetailWarningsTitle}
                message={formatResource(copy.businessDetailWarningsMessage, {
                  status: detailStatusLabel ?? detailStatus,
                })}
              />
            )}
            {matchesLocalizedQueryMessageKey(
              joinStatus,
              "loyaltyMembershipCreatedMessage",
              "joined",
            ) && (
              <StatusBanner
                title={copy.loyaltyMembershipCreatedTitle}
                message={copy.loyaltyMembershipCreatedMessage}
              />
            )}
            {resolvedJoinError && (
              <StatusBanner
                tone="warning"
                title={copy.loyaltyJoinFailedTitle}
                message={resolvedJoinError}
              />
            )}
          </div>
        )}

        <div className="grid gap-6 lg:grid-cols-[minmax(0,1fr)_340px]">
          <div className="flex flex-col gap-6">
            <div className="rounded-[1rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-6 shadow-[var(--shadow-panel)]">
              <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-brand)]">
                {copy.programSnapshotTitle}
              </p>
              <h2 className="mt-3 text-2xl font-semibold text-[var(--color-text-primary)]">
                {detail?.loyaltyProgramPublic?.name ?? copy.publishedLoyaltyProgram}
              </h2>
              <div className="mt-5 text-sm leading-7 text-[var(--color-text-secondary)]">
                <p>
                  {detail?.loyaltyProgramPublic
                    ? copy.programSnapshotDescription
                    : copy.noActiveLoyaltyProgramMessage}
                </p>
              </div>

              {rewardTiers.length > 0 ? (
                <div className="mt-5 grid gap-4 md:grid-cols-2">
                  {rewardTiers.map((reward) => (
                    <article
                      key={reward.id}
                      className="rounded-[1rem] bg-[var(--color-surface-panel-strong)] px-4 py-4"
                    >
                      <div className="flex items-start justify-between gap-4">
                        <div>
                          <p className="text-sm font-semibold text-[var(--color-text-primary)]">
                            {formatRewardValue(reward)}
                          </p>
                          {reward.description ? (
                            <p className="mt-2 text-sm leading-7 text-[var(--color-text-secondary)]">
                              {reward.description}
                            </p>
                          ) : null}
                        </div>
                        <span className="rounded-full bg-[var(--color-surface-panel)] px-3 py-2 text-xs font-semibold uppercase tracking-[0.16em] text-[var(--color-accent)]">
                          {formatResource(copy.pointsValueLabel, {
                            value: reward.pointsRequired,
                          })}
                        </span>
                      </div>
                      <p className="mt-4 text-xs font-semibold uppercase tracking-[0.16em] text-[var(--color-text-secondary)]">
                        {reward.allowSelfRedemption
                          ? copy.selfRedemptionAvailable
                          : copy.staffConfirmationRequired}
                      </p>
                    </article>
                  ))}
                </div>
              ) : (
                <p className="mt-5 text-sm leading-7 text-[var(--color-text-secondary)]">
                  {copy.noPublicRewardTiersMessage}
                </p>
              )}
            </div>

            <div className="rounded-[1rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-6 shadow-[var(--shadow-panel)]">
              <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-brand)]">
                {copy.contactDetailsTitle}
              </p>
              <div className="mt-5 space-y-3 text-sm leading-7 text-[var(--color-text-secondary)]">
                {detail?.description ? <p>{detail.description}</p> : null}
                {websiteUrl ? (
                  <p>
                    {copy.websiteLabel}{" "}
                    <a
                      href={websiteUrl}
                      {...safeExternalLinkProps}
                      className="font-semibold text-[var(--color-text-primary)] underline-offset-4 hover:underline"
                    >
                      {websiteUrl}
                    </a>
                  </p>
                ) : null}
                {detail?.contactEmail ? (
                  <p>
                    {copy.emailShortLabel} {detail.contactEmail}
                  </p>
                ) : null}
                {detail?.contactPhoneE164 ? (
                  <p>
                    {copy.phoneContactLabel} {detail.contactPhoneE164}
                  </p>
                ) : null}
                {!detail?.description &&
                !websiteUrl &&
                !detail?.contactEmail &&
                !detail?.contactPhoneE164 ? (
                  <p>{copy.noPublicContactMetadataMessage}</p>
                ) : null}
              </div>
            </div>
          </div>

          <div className="flex flex-col gap-5">
            <aside className="rounded-[1rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-6 shadow-[var(--shadow-panel)]">
              <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-accent)]">
                {copy.loyaltyPublicRouteSummaryTitle}
              </p>
              <p className="mt-5 text-sm leading-7 text-[var(--color-text-secondary)]">
                {formatResource(copy.loyaltyPublicRouteSummaryMessage, {
                  detailStatus,
                  rewardCount: rewardTiers.length,
                  locationCount: locations.length,
                })}
              </p>
            </aside>

            <aside className="rounded-[1rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-6 shadow-[var(--shadow-panel)]">
              <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-accent)]">
                {copy.branchesTitle}
              </p>
              {locations.length > 0 ? (
                <div className="mt-5 flex flex-col gap-4">
                  {locations.map((location) => (
                    <article
                      key={location.businessLocationId}
                      className="rounded-[1rem] bg-[var(--color-surface-panel-strong)] px-4 py-4"
                    >
                      <p className="text-sm font-semibold text-[var(--color-text-primary)]">
                        {location.name}
                      </p>
                      <p className="mt-2 text-sm leading-7 text-[var(--color-text-secondary)]">
                        {formatLocation(location) || copy.noPublicAddressDetails}
                      </p>
                      {location.isPrimary ? (
                        <p className="mt-2 text-xs font-semibold uppercase tracking-[0.16em] text-[var(--color-accent)]">
                          {copy.primaryBranchLabel}
                        </p>
                      ) : null}
                    </article>
                  ))}
                </div>
              ) : (
                <p className="mt-5 text-sm leading-7 text-[var(--color-text-secondary)]">
                  {copy.noPublicBranchListMessage}
                </p>
              )}
            </aside>

            <aside className="rounded-[1rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-6 shadow-[var(--shadow-panel)]">
              <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-accent)]">
                {copy.joinReadinessTitle}
              </p>
              <div className="mt-5 space-y-3 text-sm leading-7 text-[var(--color-text-secondary)]">
                <p>
                  {isAuthenticated
                    ? copy.joinReadinessAuthenticated
                    : copy.joinReadinessAnonymous}
                </p>
                <p>{copy.joinReadinessContractNote}</p>
              </div>
            </aside>

            <aside className="rounded-[1rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-6 shadow-[var(--shadow-panel)]">
              <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-brand)]">
                {copy.loyaltyPublicStorefrontWindowTitle}
              </p>
              <p className="mt-3 text-sm leading-7 text-[var(--color-text-secondary)]">
                {formatResource(copy.loyaltyPublicStorefrontWindowMessage, {
                  cmsStatus: cmsPagesStatusLabel ?? cmsPagesStatus,
                  categoriesStatus: categoriesStatusLabel ?? categoriesStatus,
                  pageCount: cmsPages.length,
                  categoryCount: categories.length,
                })}
              </p>
              <div className="mt-5 grid gap-4">
                <div className="rounded-[1rem] bg-[var(--color-surface-panel-strong)] px-4 py-4">
                  <div className="flex items-center justify-between gap-3">
                    <p className="text-sm font-semibold text-[var(--color-text-primary)]">
                      {copy.loyaltyPublicStorefrontCmsTitle}
                    </p>
                    <Link
                      href={localizeHref("/cms", culture)}
                      className="text-sm font-semibold text-[var(--color-brand)] transition hover:text-[var(--color-brand-strong)]"
                    >
                      {copy.loyaltyPublicStorefrontCmsCta}
                    </Link>
                  </div>
                  <p className="mt-3 text-sm leading-7 text-[var(--color-text-secondary)]">
                    {copy.loyaltyPublicStorefrontCmsFallbackDescription}
                  </p>
                  {cmsPages.length > 0 ? (
                    <div className="mt-4 flex flex-wrap gap-3">
                      {cmsPages.map((page) => (
                        <Link
                          key={page.id}
                    href={localizeHref(buildCmsPagePath(page.slug), culture)}
                          className="inline-flex rounded-full border border-[var(--color-border-soft)] px-3 py-2 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel)]"
                        >
                          {page.title}
                        </Link>
                      ))}
                    </div>
                  ) : (
                    <p className="mt-4 text-sm leading-7 text-[var(--color-text-secondary)]">
                      {formatResource(copy.loyaltyPublicStorefrontCmsEmptyMessage, {
                        status: cmsPagesStatusLabel ?? cmsPagesStatus,
                      })}
                    </p>
                  )}
                </div>

                <div className="rounded-[1rem] bg-[var(--color-surface-panel-strong)] px-4 py-4">
                  <div className="flex items-center justify-between gap-3">
                    <p className="text-sm font-semibold text-[var(--color-text-primary)]">
                      {copy.loyaltyPublicStorefrontCatalogTitle}
                    </p>
                    <Link
                      href={localizeHref("/catalog", culture)}
                      className="text-sm font-semibold text-[var(--color-brand)] transition hover:text-[var(--color-brand-strong)]"
                    >
                      {copy.loyaltyPublicStorefrontCatalogCta}
                    </Link>
                  </div>
                  <p className="mt-3 text-sm leading-7 text-[var(--color-text-secondary)]">
                    {copy.loyaltyPublicStorefrontCatalogFallbackDescription}
                  </p>
                  {categories.length > 0 ? (
                    <div className="mt-4 flex flex-wrap gap-3">
                      {categories.map((category) => (
                        <Link
                          key={category.id}
                          href={localizeHref(
                            buildAppQueryPath("/catalog", {
                              category: category.slug,
                            }),
                            culture,
                          )}
                          className="inline-flex rounded-full border border-[var(--color-border-soft)] px-3 py-2 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel)]"
                        >
                          {category.name}
                        </Link>
                      ))}
                    </div>
                  ) : (
                    <p className="mt-4 text-sm leading-7 text-[var(--color-text-secondary)]">
                      {formatResource(copy.loyaltyPublicStorefrontCatalogEmptyMessage, {
                        status: categoriesStatusLabel ?? categoriesStatus,
                      })}
                    </p>
                  )}
                </div>
              </div>
            </aside>

            <aside className="rounded-[1rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-6 shadow-[var(--shadow-panel)]">
              <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-accent)]">
                {copy.memberCrossSurfaceTitle}
              </p>
              <p className="mt-3 text-sm leading-7 text-[var(--color-text-secondary)]">
                {copy.memberCrossSurfaceMessage}
              </p>
              <div className="mt-5 flex flex-wrap gap-3">
                <Link href={localizeHref("/", culture)} className="inline-flex rounded-full border border-[var(--color-border-soft)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel-strong)]">
                  {copy.memberCrossSurfaceHomeCta}
                </Link>
                <Link href={localizeHref("/catalog", culture)} className="inline-flex rounded-full border border-[var(--color-border-soft)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel-strong)]">
                  {copy.memberCrossSurfaceCatalogCta}
                </Link>
                <Link href={localizeHref("/account", culture)} className="inline-flex rounded-full border border-[var(--color-border-soft)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel-strong)]">
                  {copy.memberCrossSurfaceAccountCta}
                </Link>
              </div>
            </aside>
          </div>
        </div>
      </div>
    </section>
  );
}
