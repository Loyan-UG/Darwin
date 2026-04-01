import Link from "next/link";
import { StatusBanner } from "@/components/feedback/status-banner";
import type { BusinessLocation } from "@/features/businesses/types";
import {
  clearMemberLoyaltyScanSessionAction,
  prepareMemberLoyaltyScanSessionAction,
  trackMemberPromotionInteractionAction,
} from "@/features/member-portal/actions";
import type {
  LoyaltyBusinessDashboard,
  PreparedMemberLoyaltyScanSession,
  LoyaltyRewardSummary,
  LoyaltyTimelineEntry,
  LoyaltyTimelinePage,
  MyPromotionsResponse,
  PromotionFeedItem,
} from "@/features/member-portal/types";
import { formatDateTime } from "@/lib/formatting";

type LoyaltyBusinessPageProps = {
  culture: string;
  businessId: string;
  businessLocations: BusinessLocation[];
  dashboard: LoyaltyBusinessDashboard | null;
  dashboardStatus: string;
  rewards: LoyaltyRewardSummary[] | null;
  rewardsStatus: string;
  timeline: LoyaltyTimelinePage | null;
  timelineStatus: string;
  promotions: MyPromotionsResponse | null;
  promotionsStatus: string;
  preparedScanSession: PreparedMemberLoyaltyScanSession | null;
  qrCodeDataUrl: string | null;
  scanStatus?: string;
  scanError?: string;
  promotionStatus?: string;
  promotionError?: string;
};

function formatTimelineKind(kind: string | number) {
  if (kind === 1 || kind === "RewardRedemption") {
    return "Reward redemption";
  }

  return "Points transaction";
}

function getTimelineValue(entry: LoyaltyTimelineEntry) {
  if (entry.pointsSpent) {
    return `-${entry.pointsSpent} pts`;
  }

  if (typeof entry.pointsDelta === "number") {
    return `${entry.pointsDelta >= 0 ? "+" : ""}${entry.pointsDelta} pts`;
  }

  return "Activity";
}

function getPromotionActionLabel(item: PromotionFeedItem) {
  switch (item.ctaKind) {
    case "Claim":
      return "Claim offer";
    case "OpenRewards":
      return "Open rewards";
    case "ScanNow":
      return "Open loyalty";
    default:
      return "Open promotion";
  }
}

function getPromotionEventType(item: PromotionFeedItem) {
  return item.ctaKind === "Claim" ? "Claim" : "Open";
}

export function LoyaltyBusinessPage({
  culture,
  businessId,
  businessLocations,
  dashboard,
  dashboardStatus,
  rewards,
  rewardsStatus,
  timeline,
  timelineStatus,
  promotions,
  promotionsStatus,
  preparedScanSession,
  qrCodeDataUrl,
  scanStatus,
  scanError,
  promotionStatus,
  promotionError,
}: LoyaltyBusinessPageProps) {
  const businessName = dashboard?.account.businessName ?? "Selected loyalty business";
  const progressPercent = Math.max(
    0,
    Math.min(100, Number(dashboard?.nextRewardProgressPercent ?? 0)),
  );
  const loadMoreHref =
    timeline?.nextBeforeAtUtc && timeline?.nextBeforeId
      ? `/loyalty/${businessId}?beforeAtUtc=${encodeURIComponent(
          timeline.nextBeforeAtUtc,
        )}&beforeId=${encodeURIComponent(timeline.nextBeforeId)}`
      : null;
  const selectableRewards =
    rewards?.filter((reward) => reward.isActive && reward.isSelectable) ?? [];

  return (
    <section className="mx-auto flex w-full max-w-[var(--content-max-width)] flex-1 px-5 py-12 sm:px-6 lg:px-8">
      <div className="flex w-full flex-col gap-8">
        <div className="rounded-[2rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-8 shadow-[var(--shadow-panel)] sm:px-8 sm:py-10">
          <div className="flex flex-wrap items-start justify-between gap-5">
            <div>
              <p className="text-xs font-semibold uppercase tracking-[0.26em] text-[var(--color-brand)]">
                Loyalty business
              </p>
              <h1 className="mt-4 font-[family-name:var(--font-display)] text-4xl leading-tight text-[var(--color-text-primary)] sm:text-5xl">
                {businessName}
              </h1>
              <p className="mt-5 max-w-3xl text-base leading-8 text-[var(--color-text-secondary)] sm:text-lg">
                Business-scoped loyalty progress, rewards, and recent member activity now read directly from the member portal contracts.
              </p>
            </div>
            <Link
              href="/loyalty"
              className="inline-flex rounded-full border border-[var(--color-border-soft)] px-5 py-3 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel-strong)]"
            >
              Back to loyalty
            </Link>
          </div>
        </div>

        {(dashboardStatus !== "ok" || rewardsStatus !== "ok" || timelineStatus !== "ok" || promotionsStatus !== "ok") && (
          <StatusBanner
            tone="warning"
            title="Loyalty detail loaded with warnings."
            message={`Dashboard: ${dashboardStatus}. Rewards: ${rewardsStatus}. Timeline: ${timelineStatus}. Promotions: ${promotionsStatus}.`}
          />
        )}

        {promotionStatus === "tracked" && (
          <StatusBanner
            title="Promotion interaction recorded."
            message="The promotion interaction was sent through the canonical member promotions tracking contract."
          />
        )}

        {promotionError && (
          <StatusBanner
            tone="warning"
            title="Promotion interaction failed."
            message={promotionError}
          />
        )}

        {scanStatus === "prepared" && (
          <StatusBanner
            title="Scan session prepared."
            message="A short-lived browser scan token is now ready for this business and can be used for accrual or redemption handoff."
          />
        )}

        {scanStatus === "cleared" && (
          <StatusBanner
            title="Prepared scan session cleared."
            message="The browser-owned scan preparation state was removed for this business."
          />
        )}

        {scanError && (
          <StatusBanner
            tone="warning"
            title="Scan preparation failed."
            message={scanError}
          />
        )}

        {!dashboard ? (
          <div className="rounded-[2rem] border border-dashed border-[var(--color-border-strong)] bg-[var(--color-surface-panel)] px-6 py-10 text-center text-sm leading-7 text-[var(--color-text-secondary)]">
            No business-scoped loyalty dashboard could be loaded for this business.
          </div>
        ) : (
          <>
            <div className="grid gap-5 md:grid-cols-2 xl:grid-cols-4">
              {[
                { label: "Points balance", value: String(dashboard.account.pointsBalance) },
                { label: "Lifetime points", value: String(dashboard.account.lifetimePoints) },
                { label: "Available rewards", value: String(dashboard.availableRewardsCount) },
                { label: "Redeemable now", value: String(dashboard.redeemableRewardsCount) },
              ].map((item) => (
                <article
                  key={item.label}
                  className="rounded-[2rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-6 shadow-[var(--shadow-panel)]"
                >
                  <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-accent)]">
                    {item.label}
                  </p>
                  <p className="mt-4 text-3xl font-semibold text-[var(--color-text-primary)]">
                    {item.value}
                  </p>
                </article>
              ))}
            </div>

            <div className="grid gap-6 lg:grid-cols-[minmax(0,1fr)_340px]">
              <div className="flex flex-col gap-6">
                <div className="rounded-[2rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-6 shadow-[var(--shadow-panel)]">
                  <div className="flex flex-wrap items-start justify-between gap-4">
                    <div>
                      <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-brand)]">
                        Reward progress
                      </p>
                      <h2 className="mt-3 text-2xl font-semibold text-[var(--color-text-primary)]">
                        {dashboard.nextReward?.name ?? dashboard.account.nextRewardTitle ?? "No next reward published"}
                      </h2>
                    </div>
                    <p className="rounded-full bg-[var(--color-surface-panel-strong)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)]">
                      {dashboard.account.status}
                    </p>
                  </div>

                  <div className="mt-5 space-y-3 text-sm leading-7 text-[var(--color-text-secondary)]">
                    {dashboard.pointsToNextReward !== null && dashboard.pointsToNextReward !== undefined ? (
                      <p>{dashboard.pointsToNextReward} more points needed to unlock the next reward.</p>
                    ) : (
                      <p>No additional reward threshold is currently published for this account.</p>
                    )}
                    {dashboard.nextRewardRequiredPoints ? (
                      <p>Next threshold: {dashboard.nextRewardRequiredPoints} points</p>
                    ) : null}
                    {dashboard.account.lastAccrualAtUtc ? (
                      <p>Last accrual: {formatDateTime(dashboard.account.lastAccrualAtUtc, culture)}</p>
                    ) : null}
                    {dashboard.expiryTrackingEnabled ? (
                      <p>
                        {dashboard.pointsExpiringSoon} points expire soon
                        {dashboard.nextPointsExpiryAtUtc
                          ? `, next expiry ${formatDateTime(dashboard.nextPointsExpiryAtUtc, culture)}`
                          : "."}
                      </p>
                    ) : (
                      <p>Point expiry tracking is not currently active for this loyalty program.</p>
                    )}
                  </div>

                  <div className="mt-6 h-3 overflow-hidden rounded-full bg-[var(--color-surface-panel-strong)]">
                    <div
                      className="h-full rounded-full bg-[var(--color-brand)]"
                      style={{ width: `${progressPercent}%` }}
                    />
                  </div>
                </div>

                <div className="rounded-[2rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-6 shadow-[var(--shadow-panel)]">
                  <div className="flex flex-wrap items-start justify-between gap-4">
                    <div>
                      <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-brand)]">
                        Browser scan prep
                      </p>
                      <h2 className="mt-3 text-2xl font-semibold text-[var(--color-text-primary)]">
                        Prepare an in-store scan session
                      </h2>
                      <p className="mt-2 text-sm leading-7 text-[var(--color-text-secondary)]">
                        This web flow now calls the canonical member scan-preparation endpoint. The returned token stays in a short-lived web cookie instead of leaking into the URL.
                      </p>
                    </div>
                    {preparedScanSession ? (
                      <form action={clearMemberLoyaltyScanSessionAction}>
                        <input type="hidden" name="businessId" value={businessId} />
                        <input type="hidden" name="returnPath" value={`/loyalty/${businessId}`} />
                        <button
                          type="submit"
                          className="inline-flex rounded-full border border-[var(--color-border-soft)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel-strong)]"
                        >
                          Clear token
                        </button>
                      </form>
                    ) : null}
                  </div>

                  <form action={prepareMemberLoyaltyScanSessionAction} className="mt-5 grid gap-5 lg:grid-cols-[minmax(0,1fr)_320px]">
                    <input type="hidden" name="businessId" value={businessId} />
                    <input type="hidden" name="returnPath" value={`/loyalty/${businessId}`} />
                    <div className="space-y-5">
                      <div className="grid gap-4 sm:grid-cols-2">
                        <label className="rounded-[1.5rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel-strong)] px-4 py-4">
                          <input
                            type="radio"
                            name="mode"
                            value="Accrual"
                            defaultChecked
                            className="mr-3"
                          />
                          <span className="text-sm font-semibold text-[var(--color-text-primary)]">
                            Accrual
                          </span>
                          <p className="mt-2 text-sm leading-7 text-[var(--color-text-secondary)]">
                            Prepare a scan that earns points during checkout or an in-store visit.
                          </p>
                        </label>
                        <label className="rounded-[1.5rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel-strong)] px-4 py-4">
                          <input
                            type="radio"
                            name="mode"
                            value="Redemption"
                            className="mr-3"
                          />
                          <span className="text-sm font-semibold text-[var(--color-text-primary)]">
                            Redemption
                          </span>
                          <p className="mt-2 text-sm leading-7 text-[var(--color-text-secondary)]">
                            Prepare a scan for spending points on one or more currently redeemable rewards.
                          </p>
                        </label>
                      </div>

                      <label className="flex flex-col gap-2 text-sm font-semibold text-[var(--color-text-primary)]">
                        Preferred branch
                        <select
                          name="businessLocationId"
                          defaultValue=""
                          className="rounded-2xl border border-[var(--color-border-soft)] bg-white/70 px-4 py-3 text-sm font-normal text-[var(--color-text-primary)] outline-none transition focus:border-[var(--color-brand)]"
                        >
                          <option value="">Let the backend infer the branch</option>
                          {businessLocations.map((location) => (
                            <option
                              key={location.businessLocationId}
                              value={location.businessLocationId}
                            >
                              {location.name}
                              {location.city ? ` - ${location.city}` : ""}
                            </option>
                          ))}
                        </select>
                      </label>

                      <div className="rounded-[1.5rem] bg-[var(--color-surface-panel-strong)] px-4 py-4">
                        <p className="text-sm font-semibold text-[var(--color-text-primary)]">
                          Reward selection for redemption mode
                        </p>
                        {selectableRewards.length > 0 ? (
                          <div className="mt-4 grid gap-3 md:grid-cols-2">
                            {selectableRewards.map((reward) => (
                              <label
                                key={reward.loyaltyRewardTierId}
                                className="rounded-[1.25rem] border border-[var(--color-border-soft)] bg-white/70 px-4 py-4 text-sm leading-7 text-[var(--color-text-secondary)]"
                              >
                                <input
                                  type="checkbox"
                                  name="selectedRewardTierIds"
                                  value={reward.loyaltyRewardTierId}
                                  className="mr-3"
                                />
                                <span className="font-semibold text-[var(--color-text-primary)]">
                                  {reward.name}
                                </span>
                                <p>{reward.requiredPoints} pts</p>
                                {reward.description ? <p>{reward.description}</p> : null}
                              </label>
                            ))}
                          </div>
                        ) : (
                          <p className="mt-3 text-sm leading-7 text-[var(--color-text-secondary)]">
                            No redeemable rewards are currently available, so the browser scan flow is effectively accrual-only right now.
                          </p>
                        )}
                      </div>

                      <button
                        type="submit"
                        className="inline-flex rounded-full bg-[var(--color-brand)] px-5 py-3 text-sm font-semibold text-[var(--color-brand-contrast)] transition hover:bg-[var(--color-brand-strong)]"
                      >
                        Prepare scan token
                      </button>
                    </div>

                    <div className="rounded-[1.75rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel-strong)] px-5 py-5">
                      <p className="text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-accent)]">
                        Active token
                      </p>
                      {preparedScanSession ? (
                        <div className="mt-4 space-y-4 text-sm leading-7 text-[var(--color-text-secondary)]">
                          {qrCodeDataUrl ? (
                            <div className="rounded-[1.5rem] bg-white/85 p-4">
                              {/* eslint-disable-next-line @next/next/no-img-element */}
                              <img
                                src={qrCodeDataUrl}
                                alt={`QR code for ${preparedScanSession.mode.toLowerCase()} scan token`}
                                className="mx-auto h-auto w-full max-w-64 rounded-[1rem]"
                              />
                            </div>
                          ) : null}
                          <div>
                            <p className="font-semibold text-[var(--color-text-primary)]">
                              {preparedScanSession.mode}
                            </p>
                            <p>
                              Expires: {formatDateTime(preparedScanSession.expiresAtUtc, culture)}
                            </p>
                            <p>
                              Current balance: {preparedScanSession.currentPointsBalance} pts
                            </p>
                          </div>
                          <div className="rounded-[1.25rem] bg-white/80 px-4 py-4">
                            <p className="text-xs font-semibold uppercase tracking-[0.14em] text-[var(--color-text-muted)]">
                              Scan token
                            </p>
                            <code className="mt-2 block break-all text-sm font-semibold text-[var(--color-text-primary)]">
                              {preparedScanSession.scanSessionToken}
                            </code>
                          </div>
                          {preparedScanSession.selectedRewards.length > 0 ? (
                            <div>
                              <p className="font-semibold text-[var(--color-text-primary)]">
                                Selected rewards
                              </p>
                              <div className="mt-2 flex flex-col gap-2">
                                {preparedScanSession.selectedRewards.map((reward) => (
                                  <div
                                    key={reward.loyaltyRewardTierId}
                                    className="rounded-[1rem] bg-white/70 px-3 py-3"
                                  >
                                    <p className="font-semibold text-[var(--color-text-primary)]">
                                      {reward.name}
                                    </p>
                                    <p>{reward.requiredPoints} pts</p>
                                  </div>
                                ))}
                              </div>
                            </div>
                          ) : null}
                          <p>
                            This QR code is rendered from the canonical short-lived scan token. The token still stays out of the URL and remains browser-local until it expires or is cleared.
                          </p>
                        </div>
                      ) : (
                        <p className="mt-4 text-sm leading-7 text-[var(--color-text-secondary)]">
                          No active browser-prepared scan token exists for this business yet.
                        </p>
                      )}
                    </div>
                  </form>
                </div>

                <div className="rounded-[2rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-6 shadow-[var(--shadow-panel)]">
                  <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-brand)]">
                    Rewards
                  </p>
                  {rewards && rewards.length > 0 ? (
                    <div className="mt-5 grid gap-4 md:grid-cols-2">
                      {rewards.map((reward) => (
                        <article
                          key={reward.loyaltyRewardTierId}
                          className="rounded-[1.5rem] bg-[var(--color-surface-panel-strong)] px-4 py-4"
                        >
                          <div className="flex items-start justify-between gap-4">
                            <div>
                              <h3 className="text-lg font-semibold text-[var(--color-text-primary)]">
                                {reward.name}
                              </h3>
                              {reward.description ? (
                                <p className="mt-2 text-sm leading-7 text-[var(--color-text-secondary)]">
                                  {reward.description}
                                </p>
                              ) : null}
                            </div>
                            <span className="rounded-full bg-[var(--color-surface-panel)] px-3 py-2 text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-accent)]">
                              {reward.requiredPoints} pts
                            </span>
                          </div>
                          <div className="mt-4 flex flex-wrap gap-2 text-xs font-semibold uppercase tracking-[0.16em] text-[var(--color-text-secondary)]">
                            <span>{reward.isSelectable ? "Redeemable" : "Locked"}</span>
                            <span>{reward.requiresConfirmation ? "Needs confirmation" : "Instant"}</span>
                            <span>{reward.isActive ? "Active" : "Inactive"}</span>
                          </div>
                        </article>
                      ))}
                    </div>
                  ) : (
                    <p className="mt-5 text-sm leading-7 text-[var(--color-text-secondary)]">
                      No published rewards are currently available for this business.
                    </p>
                  )}
                </div>

                <div id="promotions" className="rounded-[2rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-6 shadow-[var(--shadow-panel)]">
                  <div className="flex flex-wrap items-start justify-between gap-4">
                    <div>
                      <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-brand)]">
                        Promotions
                      </p>
                      <p className="mt-2 text-sm leading-7 text-[var(--color-text-secondary)]">
                        Personalized promotion cards now read from the canonical member promotions feed for this business.
                      </p>
                    </div>
                    {promotions ? (
                      <p className="rounded-full bg-[var(--color-surface-panel-strong)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)]">
                        {promotions.diagnostics.finalCount} visible
                      </p>
                    ) : null}
                  </div>

                  {promotions?.items.length ? (
                    <div className="mt-5 grid gap-4 md:grid-cols-2">
                      {promotions.items.map((item) => (
                        <article
                          key={`${item.businessId}-${item.title}-${item.ctaKind}`}
                          className="rounded-[1.5rem] bg-[var(--color-surface-panel-strong)] px-4 py-4"
                        >
                          <div className="flex items-start justify-between gap-4">
                            <div>
                              <p className="text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-accent)]">
                                {item.campaignState}
                              </p>
                              <h3 className="mt-2 text-lg font-semibold text-[var(--color-text-primary)]">
                                {item.title}
                              </h3>
                            </div>
                            <span className="rounded-full bg-[var(--color-surface-panel)] px-3 py-2 text-xs font-semibold uppercase tracking-[0.16em] text-[var(--color-text-primary)]">
                              P{item.priority}
                            </span>
                          </div>
                          <p className="mt-3 text-sm leading-7 text-[var(--color-text-secondary)]">
                            {item.description}
                          </p>
                          {(item.startsAtUtc || item.endsAtUtc) && (
                            <div className="mt-3 text-sm leading-7 text-[var(--color-text-secondary)]">
                              {item.startsAtUtc ? <p>Starts: {formatDateTime(item.startsAtUtc, culture)}</p> : null}
                              {item.endsAtUtc ? <p>Ends: {formatDateTime(item.endsAtUtc, culture)}</p> : null}
                            </div>
                          )}
                          {item.eligibilityRules.length > 0 && (
                            <div className="mt-3 flex flex-wrap gap-2">
                              {item.eligibilityRules.slice(0, 3).map((rule, index) => (
                                <span
                                  key={`${item.title}-${rule.audienceKind}-${index}`}
                                  className="rounded-full border border-[var(--color-border-soft)] px-3 py-2 text-xs font-semibold uppercase tracking-[0.14em] text-[var(--color-text-secondary)]"
                                >
                                  {rule.tierKey ?? rule.audienceKind}
                                </span>
                              ))}
                            </div>
                          )}
                          <form action={trackMemberPromotionInteractionAction} className="mt-5">
                            <input type="hidden" name="businessId" value={item.businessId} />
                            <input type="hidden" name="businessName" value={item.businessName} />
                            <input type="hidden" name="title" value={item.title} />
                            <input type="hidden" name="ctaKind" value={item.ctaKind} />
                            <input type="hidden" name="eventType" value={getPromotionEventType(item)} />
                            <input type="hidden" name="returnPath" value={`/loyalty/${businessId}`} />
                            <button
                              type="submit"
                              className="inline-flex rounded-full border border-[var(--color-border-soft)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel)]"
                            >
                              {getPromotionActionLabel(item)}
                            </button>
                          </form>
                        </article>
                      ))}
                    </div>
                  ) : (
                    <p className="mt-5 text-sm leading-7 text-[var(--color-text-secondary)]">
                      No promotion cards are currently available for this business.
                    </p>
                  )}
                </div>

                <div className="rounded-[2rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-6 shadow-[var(--shadow-panel)]">
                  <div className="flex flex-wrap items-start justify-between gap-4">
                    <div>
                      <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-brand)]">
                        Unified timeline
                      </p>
                      <p className="mt-2 text-sm leading-7 text-[var(--color-text-secondary)]">
                        Recent loyalty events across points transactions and reward redemptions.
                      </p>
                    </div>
                    {loadMoreHref ? (
                      <Link
                        href={loadMoreHref}
                        className="inline-flex rounded-full border border-[var(--color-border-soft)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel-strong)]"
                      >
                        Older activity
                      </Link>
                    ) : null}
                  </div>

                  {timeline && timeline.items.length > 0 ? (
                    <div className="mt-5 flex flex-col gap-4">
                      {timeline.items.map((entry) => (
                        <article
                          key={`${entry.id}-${entry.occurredAtUtc}`}
                          className="rounded-[1.5rem] bg-[var(--color-surface-panel-strong)] px-4 py-4"
                        >
                          <div className="flex flex-wrap items-start justify-between gap-4">
                            <div>
                              <p className="text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-accent)]">
                                {formatTimelineKind(entry.kind)}
                              </p>
                              <p className="mt-2 text-sm leading-7 text-[var(--color-text-secondary)]">
                                {entry.note ?? entry.reference ?? "No additional note provided."}
                              </p>
                              <p className="mt-1 text-sm leading-7 text-[var(--color-text-secondary)]">
                                {formatDateTime(entry.occurredAtUtc, culture)}
                              </p>
                            </div>
                            <p className="text-sm font-semibold text-[var(--color-text-primary)]">
                              {getTimelineValue(entry)}
                            </p>
                          </div>
                        </article>
                      ))}
                    </div>
                  ) : (
                    <p className="mt-5 text-sm leading-7 text-[var(--color-text-secondary)]">
                      No recent loyalty timeline entries are currently available for this business.
                    </p>
                  )}
                </div>
              </div>

              <div className="flex flex-col gap-5">
                <aside className="rounded-[2rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-6 shadow-[var(--shadow-panel)]">
                  <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-accent)]">
                    Account details
                  </p>
                  <div className="mt-5 space-y-3 text-sm leading-7 text-[var(--color-text-secondary)]">
                    <div className="flex items-center justify-between gap-4">
                      <span>Status</span>
                      <span>{dashboard.account.status}</span>
                    </div>
                    <div className="flex items-center justify-between gap-4">
                      <span>Loyalty account</span>
                      <span className="truncate">{dashboard.account.loyaltyAccountId}</span>
                    </div>
                    <div className="flex items-center justify-between gap-4">
                      <span>Business id</span>
                      <span className="truncate">{dashboard.account.businessId}</span>
                    </div>
                  </div>
                </aside>

                <aside className="rounded-[2rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-6 shadow-[var(--shadow-panel)]">
                  <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-accent)]">
                    Recent transactions
                  </p>
                  {dashboard.recentTransactions.length > 0 ? (
                    <div className="mt-5 flex flex-col gap-4">
                      {dashboard.recentTransactions.map((transaction) => (
                        <article
                          key={`${transaction.occurredAtUtc}-${transaction.type}-${transaction.delta}`}
                          className="rounded-[1.5rem] bg-[var(--color-surface-panel-strong)] px-4 py-4"
                        >
                          <div className="flex items-start justify-between gap-4">
                            <div>
                              <p className="text-sm font-semibold text-[var(--color-text-primary)]">
                                {transaction.type}
                              </p>
                              <p className="mt-1 text-sm leading-7 text-[var(--color-text-secondary)]">
                                {formatDateTime(transaction.occurredAtUtc, culture)}
                              </p>
                              {transaction.notes ? (
                                <p className="mt-1 text-sm leading-7 text-[var(--color-text-secondary)]">
                                  {transaction.notes}
                                </p>
                              ) : null}
                            </div>
                            <p className="text-sm font-semibold text-[var(--color-text-primary)]">
                              {transaction.delta >= 0 ? "+" : ""}
                              {transaction.delta} pts
                            </p>
                          </div>
                        </article>
                      ))}
                    </div>
                  ) : (
                    <p className="mt-5 text-sm leading-7 text-[var(--color-text-secondary)]">
                      No recent transactions are currently available for this business.
                    </p>
                  )}
                </aside>

                {promotions && (
                  <aside className="rounded-[2rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-6 shadow-[var(--shadow-panel)]">
                    <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-accent)]">
                      Feed diagnostics
                    </p>
                    <div className="mt-5 space-y-3 text-sm leading-7 text-[var(--color-text-secondary)]">
                      <div className="flex items-center justify-between gap-4">
                        <span>Initial candidates</span>
                        <span>{promotions.diagnostics.initialCandidates}</span>
                      </div>
                      <div className="flex items-center justify-between gap-4">
                        <span>Suppressed</span>
                        <span>{promotions.diagnostics.suppressedByFrequency}</span>
                      </div>
                      <div className="flex items-center justify-between gap-4">
                        <span>Deduplicated</span>
                        <span>{promotions.diagnostics.deduplicated}</span>
                      </div>
                      <div className="flex items-center justify-between gap-4">
                        <span>Trimmed by cap</span>
                        <span>{promotions.diagnostics.trimmedByCap}</span>
                      </div>
                    </div>
                  </aside>
                )}
              </div>
            </div>
          </>
        )}
      </div>
    </section>
  );
}
