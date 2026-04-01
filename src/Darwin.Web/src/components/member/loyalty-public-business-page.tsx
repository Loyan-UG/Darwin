import Link from "next/link";
import { StatusBanner } from "@/components/feedback/status-banner";
import { joinMemberLoyaltyBusinessAction } from "@/features/member-portal/actions";
import type {
  BusinessDetail,
  BusinessLocation,
  LoyaltyRewardTierPublic,
} from "@/features/businesses/types";

type LoyaltyPublicBusinessPageProps = {
  businessId: string;
  detail: BusinessDetail | null;
  detailStatus: string;
  isAuthenticated: boolean;
  joinStatus?: string;
  joinError?: string;
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
  businessId,
  detail,
  detailStatus,
  isAuthenticated,
  joinStatus,
  joinError,
}: LoyaltyPublicBusinessPageProps) {
  const locations = detail?.locations ?? [];
  const rewardTiers = detail?.loyaltyProgramPublic?.rewardTiers ?? [];
  const heroImage =
    detail?.primaryImageUrl ?? detail?.galleryImageUrls?.[0] ?? detail?.imageUrls?.[0];

  return (
    <section className="mx-auto flex w-full max-w-[var(--content-max-width)] flex-1 px-5 py-12 sm:px-6 lg:px-8">
      <div className="flex w-full flex-col gap-8">
        <div className="overflow-hidden rounded-[2rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] shadow-[var(--shadow-panel)]">
          <div className="grid gap-0 lg:grid-cols-[minmax(0,1.15fr)_minmax(320px,0.85fr)]">
            <div className="px-6 py-8 sm:px-8 sm:py-10">
              <p className="text-xs font-semibold uppercase tracking-[0.26em] text-[var(--color-brand)]">
                Loyalty place
              </p>
              <h1 className="mt-4 font-[family-name:var(--font-display)] text-4xl leading-tight text-[var(--color-text-primary)] sm:text-5xl">
                {detail?.name ?? "Loyalty business"}
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
                    Loyalty active
                  </span>
                ) : null}
              </div>
              <p className="mt-5 max-w-3xl text-base leading-8 text-[var(--color-text-secondary)] sm:text-lg">
                {detail?.shortDescription ??
                  detail?.description ??
                  "This business detail now reads from the public business contract, so discovery and pre-join loyalty UX can stay on the canonical platform surface."}
              </p>

              <div className="mt-8 flex flex-wrap gap-3">
                {isAuthenticated ? (
                  <form action={joinMemberLoyaltyBusinessAction}>
                    <input type="hidden" name="businessId" value={businessId} />
                    <input type="hidden" name="returnPath" value={`/loyalty/${businessId}`} />
                    {locations.length > 0 ? (
                      <select
                        name="businessLocationId"
                        defaultValue=""
                        className="mb-3 block w-full rounded-2xl border border-[var(--color-border-soft)] bg-white/70 px-4 py-3 text-sm text-[var(--color-text-primary)] outline-none transition focus:border-[var(--color-brand)]"
                      >
                        <option value="">Join without selecting a preferred branch</option>
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
                      Join loyalty
                    </button>
                  </form>
                ) : (
                  <div className="flex flex-wrap gap-3">
                    <Link
                      href={`/account/sign-in?returnPath=${encodeURIComponent(`/loyalty/${businessId}`)}`}
                      className="inline-flex rounded-full bg-[var(--color-brand)] px-5 py-3 text-sm font-semibold text-[var(--color-brand-contrast)] transition hover:bg-[var(--color-brand-strong)]"
                    >
                      Sign in to join
                    </Link>
                    <Link
                      href="/account/register"
                      className="inline-flex rounded-full border border-[var(--color-border-soft)] px-5 py-3 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel-strong)]"
                    >
                      Create account
                    </Link>
                  </div>
                )}
                <Link
                  href="/loyalty"
                  className="inline-flex rounded-full border border-[var(--color-border-soft)] px-5 py-3 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel-strong)]"
                >
                  Back to loyalty
                </Link>
              </div>
            </div>

            <div className="flex min-h-72 items-center justify-center bg-[linear-gradient(145deg,rgba(228,240,212,0.95),rgba(255,253,248,1))] p-8">
              {heroImage ? (
                // eslint-disable-next-line @next/next/no-img-element
                <img
                  src={heroImage}
                  alt={detail?.name ?? "Business media"}
                  className="max-h-72 w-auto object-contain"
                />
              ) : (
                <div className="text-center">
                  <p className="text-xs font-semibold uppercase tracking-[0.2em] text-[var(--color-text-muted)]">
                    {detail?.category ?? "Loyalty business"}
                  </p>
                  <p className="mt-2 text-sm text-[var(--color-text-secondary)]">
                    No business media
                  </p>
                </div>
              )}
            </div>
          </div>
        </div>

        {(detailStatus !== "ok" || joinError || joinStatus === "joined") && (
          <div className="flex flex-col gap-3">
            {detailStatus !== "ok" && (
              <StatusBanner
                tone="warning"
                title="Business detail loaded with warnings."
                message={`Business detail status: ${detailStatus}. Public loyalty discovery remains visible instead of collapsing into a blank route.`}
              />
            )}
            {joinStatus === "joined" && (
              <StatusBanner
                title="Loyalty membership created."
                message="The canonical member join contract completed. This route should now fall through to the full member dashboard on reload."
              />
            )}
            {joinError && (
              <StatusBanner
                tone="warning"
                title="Loyalty join failed."
                message={joinError}
              />
            )}
          </div>
        )}

        <div className="grid gap-6 lg:grid-cols-[minmax(0,1fr)_340px]">
          <div className="flex flex-col gap-6">
            <div className="rounded-[2rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-6 shadow-[var(--shadow-panel)]">
              <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-brand)]">
                Program snapshot
              </p>
              <h2 className="mt-3 text-2xl font-semibold text-[var(--color-text-primary)]">
                {detail?.loyaltyProgramPublic?.name ?? "Published loyalty program"}
              </h2>
              <div className="mt-5 text-sm leading-7 text-[var(--color-text-secondary)]">
                <p>
                  {detail?.loyaltyProgramPublic
                    ? "Reward tiers now come from the public loyalty-program contract, so pre-join storefront UX can explain value before the member enrolls."
                    : "No active public loyalty program was returned for this business."}
                </p>
              </div>

              {rewardTiers.length > 0 ? (
                <div className="mt-5 grid gap-4 md:grid-cols-2">
                  {rewardTiers.map((reward) => (
                    <article
                      key={reward.id}
                      className="rounded-[1.5rem] bg-[var(--color-surface-panel-strong)] px-4 py-4"
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
                          {reward.pointsRequired} pts
                        </span>
                      </div>
                      <p className="mt-4 text-xs font-semibold uppercase tracking-[0.16em] text-[var(--color-text-secondary)]">
                        {reward.allowSelfRedemption
                          ? "Self redemption available"
                          : "Staff confirmation required"}
                      </p>
                    </article>
                  ))}
                </div>
              ) : (
                <p className="mt-5 text-sm leading-7 text-[var(--color-text-secondary)]">
                  No public reward tiers are currently published for this business.
                </p>
              )}
            </div>

            <div className="rounded-[2rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-6 shadow-[var(--shadow-panel)]">
              <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-brand)]">
                Contact and details
              </p>
              <div className="mt-5 space-y-3 text-sm leading-7 text-[var(--color-text-secondary)]">
                {detail?.description ? <p>{detail.description}</p> : null}
                {detail?.websiteUrl ? (
                  <p>
                    Website:{" "}
                    <a
                      href={detail.websiteUrl}
                      target="_blank"
                      rel="noreferrer"
                      className="font-semibold text-[var(--color-text-primary)] underline-offset-4 hover:underline"
                    >
                      {detail.websiteUrl}
                    </a>
                  </p>
                ) : null}
                {detail?.contactEmail ? <p>Email: {detail.contactEmail}</p> : null}
                {detail?.contactPhoneE164 ? (
                  <p>Phone: {detail.contactPhoneE164}</p>
                ) : null}
                {!detail?.description &&
                !detail?.websiteUrl &&
                !detail?.contactEmail &&
                !detail?.contactPhoneE164 ? (
                  <p>No richer public contact metadata was returned for this business.</p>
                ) : null}
              </div>
            </div>
          </div>

          <div className="flex flex-col gap-5">
            <aside className="rounded-[2rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-6 shadow-[var(--shadow-panel)]">
              <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-accent)]">
                Branches
              </p>
              {locations.length > 0 ? (
                <div className="mt-5 flex flex-col gap-4">
                  {locations.map((location) => (
                    <article
                      key={location.businessLocationId}
                      className="rounded-[1.5rem] bg-[var(--color-surface-panel-strong)] px-4 py-4"
                    >
                      <p className="text-sm font-semibold text-[var(--color-text-primary)]">
                        {location.name}
                      </p>
                      <p className="mt-2 text-sm leading-7 text-[var(--color-text-secondary)]">
                        {formatLocation(location) || "No public address details"}
                      </p>
                      {location.isPrimary ? (
                        <p className="mt-2 text-xs font-semibold uppercase tracking-[0.16em] text-[var(--color-accent)]">
                          Primary branch
                        </p>
                      ) : null}
                    </article>
                  ))}
                </div>
              ) : (
                <p className="mt-5 text-sm leading-7 text-[var(--color-text-secondary)]">
                  No public branch list is currently available for this business.
                </p>
              )}
            </aside>

            <aside className="rounded-[2rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-6 shadow-[var(--shadow-panel)]">
              <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-accent)]">
                Join readiness
              </p>
              <div className="mt-5 space-y-3 text-sm leading-7 text-[var(--color-text-secondary)]">
                <p>
                  {isAuthenticated
                    ? "You can create the loyalty account directly from this page."
                    : "Sign in first to create a loyalty account for this business."}
                </p>
                <p>
                  The join action uses the canonical member loyalty contract and does not depend on WebAdmin DTOs or a web-local enrollment workflow.
                </p>
              </div>
            </aside>
          </div>
        </div>
      </div>
    </section>
  );
}
