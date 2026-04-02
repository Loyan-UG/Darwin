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
  LoyaltyRewardSummary,
  LoyaltyTimelineEntry,
  LoyaltyTimelinePage,
  MyPromotionsResponse,
  PreparedMemberLoyaltyScanSession,
  PromotionFeedItem,
} from "@/features/member-portal/types";
import {
  formatResource,
  getMemberResource,
  resolveLocalizedQueryMessage,
} from "@/localization";
import { formatDateTime } from "@/lib/formatting";
import { localizeHref } from "@/lib/locale-routing";

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

function getPromotionLabel(
  item: PromotionFeedItem,
  copy: ReturnType<typeof getMemberResource>,
) {
  switch (item.ctaKind) {
    case "Claim":
      return copy.claimOfferCta;
    case "OpenRewards":
      return copy.openRewardsCta;
    case "ScanNow":
      return copy.openLoyaltyCta;
    default:
      return copy.openPromotionCta;
  }
}

function getPromotionEventType(item: PromotionFeedItem) {
  return item.ctaKind === "Claim" ? "Claim" : "Open";
}

function getTimelineKind(
  entry: LoyaltyTimelineEntry,
  copy: ReturnType<typeof getMemberResource>,
) {
  return entry.kind === 1 || entry.kind === "RewardRedemption"
    ? copy.rewardRedemptionKind
    : copy.pointsTransactionKind;
}

function getTimelineValue(
  entry: LoyaltyTimelineEntry,
  copy: ReturnType<typeof getMemberResource>,
) {
  if (entry.pointsSpent) return `-${entry.pointsSpent} pts`;
  if (typeof entry.pointsDelta === "number") {
    return `${entry.pointsDelta >= 0 ? "+" : ""}${entry.pointsDelta} pts`;
  }

  return copy.activityFallback;
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
  const copy = getMemberResource(culture);
  const resolvedScanError = resolveLocalizedQueryMessage(scanError, copy);
  const resolvedPromotionError = resolveLocalizedQueryMessage(
    promotionError,
    copy,
  );
  const name = dashboard?.account.businessName ?? copy.selectedLoyaltyBusinessFallback;
  const selectedRewards =
    rewards?.filter((reward) => reward.isActive && reward.isSelectable) ?? [];
  const loadMoreHref =
    timeline?.nextBeforeAtUtc && timeline?.nextBeforeId
      ? localizeHref(
          `/loyalty/${businessId}?beforeAtUtc=${encodeURIComponent(timeline.nextBeforeAtUtc)}&beforeId=${encodeURIComponent(timeline.nextBeforeId)}`,
          culture,
        )
      : null;
  const expirySuffix = dashboard?.nextPointsExpiryAtUtc
    ? formatResource(copy.nextExpirySuffix, {
        value: formatDateTime(dashboard.nextPointsExpiryAtUtc, culture),
      })
    : ".";

  return (
    <section className="mx-auto flex w-full max-w-[var(--content-max-width)] flex-1 px-5 py-12 sm:px-6 lg:px-8">
      <div className="flex w-full flex-col gap-8">
        <div className="rounded-[2rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-8 shadow-[var(--shadow-panel)] sm:px-8">
          <div className="flex flex-wrap items-start justify-between gap-4">
            <div>
              <p className="text-xs font-semibold uppercase tracking-[0.26em] text-[var(--color-brand)]">
                {copy.loyaltyBusinessFallback}
              </p>
              <h1 className="mt-4 font-[family-name:var(--font-display)] text-4xl text-[var(--color-text-primary)] sm:text-5xl">
                {name}
              </h1>
              <p className="mt-4 max-w-3xl text-base leading-8 text-[var(--color-text-secondary)]">
                {copy.loyaltyOverviewDescriptionSignedIn}
              </p>
            </div>
            <Link
              href={localizeHref("/loyalty", culture)}
              className="inline-flex rounded-full border border-[var(--color-border-soft)] px-5 py-3 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel-strong)]"
            >
              {copy.backToLoyaltyCta}
            </Link>
          </div>
        </div>

        {(dashboardStatus !== "ok" ||
          rewardsStatus !== "ok" ||
          timelineStatus !== "ok" ||
          promotionsStatus !== "ok") && (
          <StatusBanner
            tone="warning"
            title={copy.loyaltyDetailWarningsTitle}
            message={formatResource(copy.loyaltyDetailWarningsMessage, {
              dashboardStatus,
              rewardsStatus,
              timelineStatus,
              promotionsStatus,
            })}
          />
        )}
        {promotionStatus === "tracked" && (
          <StatusBanner title={copy.promotionTrackedTitle} message={copy.promotionTrackedMessage} />
        )}
        {resolvedPromotionError && (
          <StatusBanner
            tone="warning"
            title={copy.promotionFailedTitle}
            message={resolvedPromotionError}
          />
        )}
        {scanStatus === "prepared" && (
          <StatusBanner title={copy.scanPreparedTitle} message={copy.scanPreparedMessage} />
        )}
        {scanStatus === "cleared" && (
          <StatusBanner title={copy.scanClearedTitle} message={copy.scanClearedMessage} />
        )}
        {resolvedScanError && (
          <StatusBanner
            tone="warning"
            title={copy.scanFailedTitle}
            message={resolvedScanError}
          />
        )}

        {!dashboard ? (
          <div className="rounded-[2rem] border border-dashed border-[var(--color-border-strong)] bg-[var(--color-surface-panel)] px-6 py-10 text-center text-sm leading-7 text-[var(--color-text-secondary)]">
            {copy.noBusinessDashboardMessage}
          </div>
        ) : (
          <>
            <div className="grid gap-5 md:grid-cols-2 xl:grid-cols-4">
              {[
                [copy.pointsBalanceLabel, dashboard.account.pointsBalance],
                [copy.lifetimePointsLabel, dashboard.account.lifetimePoints],
                [copy.availableRewardsLabel, dashboard.availableRewardsCount],
                [copy.redeemableNowLabel, dashboard.redeemableRewardsCount],
              ].map(([label, value]) => (
                <article key={label} className="rounded-[2rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-6 shadow-[var(--shadow-panel)]">
                  <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-accent)]">{label}</p>
                  <p className="mt-4 text-3xl font-semibold text-[var(--color-text-primary)]">{value}</p>
                </article>
              ))}
            </div>

            <div className="grid gap-6 lg:grid-cols-[minmax(0,1fr)_340px]">
              <div className="flex flex-col gap-6">
                <article className="rounded-[2rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-6 shadow-[var(--shadow-panel)]">
                  <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-brand)]">{copy.rewardProgressTitle}</p>
                  <h2 className="mt-3 text-2xl font-semibold text-[var(--color-text-primary)]">
                    {dashboard.nextReward?.name ?? dashboard.account.nextRewardTitle ?? copy.noNextRewardPublished}
                  </h2>
                  <div className="mt-4 space-y-3 text-sm leading-7 text-[var(--color-text-secondary)]">
                    <p>
                      {dashboard.pointsToNextReward !== null && dashboard.pointsToNextReward !== undefined
                        ? formatResource(copy.pointsNeededNextRewardMessage, { value: dashboard.pointsToNextReward })
                        : copy.noAdditionalRewardThresholdMessage}
                    </p>
                    {dashboard.nextRewardRequiredPoints ? <p>{formatResource(copy.nextThresholdLabel, { value: dashboard.nextRewardRequiredPoints })}</p> : null}
                    {dashboard.account.lastAccrualAtUtc ? <p>{formatResource(copy.lastAccrualLabel, { value: formatDateTime(dashboard.account.lastAccrualAtUtc, culture) })}</p> : null}
                    <p>
                      {dashboard.expiryTrackingEnabled
                        ? formatResource(copy.pointsExpireSoonMessage, { points: dashboard.pointsExpiringSoon, suffix: expirySuffix })
                        : copy.pointExpiryInactiveMessage}
                    </p>
                  </div>
                  <div className="mt-6 h-3 overflow-hidden rounded-full bg-[var(--color-surface-panel-strong)]">
                    <div className="h-full rounded-full bg-[var(--color-brand)]" style={{ width: `${Math.max(0, Math.min(100, Number(dashboard.nextRewardProgressPercent ?? 0)))}%` }} />
                  </div>
                </article>

                <article className="rounded-[2rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-6 shadow-[var(--shadow-panel)]">
                  <div className="flex flex-wrap items-start justify-between gap-4">
                    <div>
                      <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-brand)]">{copy.phoneVerificationEyebrow}</p>
                      <h2 className="mt-3 text-2xl font-semibold text-[var(--color-text-primary)]">{copy.browserScanPrepTitle}</h2>
                      <p className="mt-2 text-sm leading-7 text-[var(--color-text-secondary)]">{copy.browserScanPrepDescription}</p>
                    </div>
                    {preparedScanSession ? (
                      <form action={clearMemberLoyaltyScanSessionAction}>
                        <input type="hidden" name="businessId" value={businessId} />
                        <input type="hidden" name="returnPath" value={localizeHref(`/loyalty/${businessId}`, culture)} />
                        <button type="submit" className="inline-flex rounded-full border border-[var(--color-border-soft)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel-strong)]">{copy.clearTokenCta}</button>
                      </form>
                    ) : null}
                  </div>
                  <form action={prepareMemberLoyaltyScanSessionAction} className="mt-5 grid gap-5 lg:grid-cols-[minmax(0,1fr)_320px]">
                    <input type="hidden" name="businessId" value={businessId} />
                    <input type="hidden" name="returnPath" value={localizeHref(`/loyalty/${businessId}`, culture)} />
                    <div className="space-y-5">
                      <div className="grid gap-4 sm:grid-cols-2">
                        <label className="rounded-[1.5rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel-strong)] px-4 py-4">
                          <input type="radio" name="mode" value="Accrual" defaultChecked className="mr-3" />
                          <span className="text-sm font-semibold text-[var(--color-text-primary)]">{copy.accrualModeTitle}</span>
                          <p className="mt-2 text-sm leading-7 text-[var(--color-text-secondary)]">{copy.accrualModeDescription}</p>
                        </label>
                        <label className="rounded-[1.5rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel-strong)] px-4 py-4">
                          <input type="radio" name="mode" value="Redemption" className="mr-3" />
                          <span className="text-sm font-semibold text-[var(--color-text-primary)]">{copy.redemptionModeTitle}</span>
                          <p className="mt-2 text-sm leading-7 text-[var(--color-text-secondary)]">{copy.redemptionModeDescription}</p>
                        </label>
                      </div>
                      <label className="flex flex-col gap-2 text-sm font-semibold text-[var(--color-text-primary)]">
                        {copy.preferredBranchLabel}
                        <select name="businessLocationId" defaultValue="" className="rounded-2xl border border-[var(--color-border-soft)] bg-white/70 px-4 py-3 text-sm font-normal text-[var(--color-text-primary)] outline-none transition focus:border-[var(--color-brand)]">
                          <option value="">{copy.inferBranchOption}</option>
                          {businessLocations.map((location) => (
                            <option key={location.businessLocationId} value={location.businessLocationId}>
                              {location.name}{location.city ? ` - ${location.city}` : ""}
                            </option>
                          ))}
                        </select>
                      </label>
                      <div className="rounded-[1.5rem] bg-[var(--color-surface-panel-strong)] px-4 py-4">
                        <p className="text-sm font-semibold text-[var(--color-text-primary)]">{copy.rewardSelectionTitle}</p>
                        {selectedRewards.length > 0 ? (
                          <div className="mt-4 grid gap-3 md:grid-cols-2">
                            {selectedRewards.map((reward) => (
                              <label key={reward.loyaltyRewardTierId} className="rounded-[1.25rem] border border-[var(--color-border-soft)] bg-white/70 px-4 py-4 text-sm leading-7 text-[var(--color-text-secondary)]">
                                <input type="checkbox" name="selectedRewardTierIds" value={reward.loyaltyRewardTierId} className="mr-3" />
                                <span className="font-semibold text-[var(--color-text-primary)]">{reward.name}</span>
                                <p>{reward.requiredPoints} pts</p>
                                {reward.description ? <p>{reward.description}</p> : null}
                              </label>
                            ))}
                          </div>
                        ) : <p className="mt-3 text-sm leading-7 text-[var(--color-text-secondary)]">{copy.noRedeemableRewardsMessage}</p>}
                      </div>
                      <button type="submit" className="inline-flex rounded-full bg-[var(--color-brand)] px-5 py-3 text-sm font-semibold text-[var(--color-brand-contrast)] transition hover:bg-[var(--color-brand-strong)]">{copy.prepareScanTokenCta}</button>
                    </div>
                    <div className="rounded-[1.75rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel-strong)] px-5 py-5">
                      <p className="text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-accent)]">{copy.activeTokenTitle}</p>
                      {preparedScanSession ? (
                        <div className="mt-4 space-y-4 text-sm leading-7 text-[var(--color-text-secondary)]">
                          {qrCodeDataUrl ? (
                            <div className="rounded-[1.5rem] bg-white/85 p-4">
                              {/* eslint-disable-next-line @next/next/no-img-element */}
                              <img src={qrCodeDataUrl} alt={`QR code for ${preparedScanSession.mode.toLowerCase()} scan token`} className="mx-auto h-auto w-full max-w-64 rounded-[1rem]" />
                            </div>
                          ) : null}
                          <div>
                            <p className="font-semibold text-[var(--color-text-primary)]">{preparedScanSession.mode}</p>
                            <p>{formatResource(copy.expiresLabel, { value: formatDateTime(preparedScanSession.expiresAtUtc, culture) })}</p>
                            <p>{formatResource(copy.currentBalanceLabel, { value: preparedScanSession.currentPointsBalance })}</p>
                          </div>
                          <div className="rounded-[1.25rem] bg-white/80 px-4 py-4">
                            <p className="text-xs font-semibold uppercase tracking-[0.14em] text-[var(--color-text-muted)]">{copy.scanTokenLabel}</p>
                            <code className="mt-2 block break-all text-sm font-semibold text-[var(--color-text-primary)]">{preparedScanSession.scanSessionToken}</code>
                          </div>
                          {preparedScanSession.selectedRewards.length > 0 ? <div><p className="font-semibold text-[var(--color-text-primary)]">{copy.selectedRewardsTitle}</p><div className="mt-2 flex flex-col gap-2">{preparedScanSession.selectedRewards.map((reward) => <div key={reward.loyaltyRewardTierId} className="rounded-[1rem] bg-white/70 px-3 py-3"><p className="font-semibold text-[var(--color-text-primary)]">{reward.name}</p><p>{reward.requiredPoints} pts</p></div>)}</div></div> : null}
                          <p>{copy.scanTokenNote}</p>
                        </div>
                      ) : <p className="mt-4 text-sm leading-7 text-[var(--color-text-secondary)]">{copy.noActiveScanTokenMessage}</p>}
                    </div>
                  </form>
                </article>

                <article className="rounded-[2rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-6 shadow-[var(--shadow-panel)]">
                  <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-brand)]">{copy.rewardsTitle}</p>
                  {rewards?.length ? (
                    <div className="mt-5 grid gap-4 md:grid-cols-2">
                      {rewards.map((reward) => (
                        <article key={reward.loyaltyRewardTierId} className="rounded-[1.5rem] bg-[var(--color-surface-panel-strong)] px-4 py-4">
                          <div className="flex items-start justify-between gap-4">
                            <div>
                              <h3 className="text-lg font-semibold text-[var(--color-text-primary)]">{reward.name}</h3>
                              {reward.description ? <p className="mt-2 text-sm leading-7 text-[var(--color-text-secondary)]">{reward.description}</p> : null}
                            </div>
                            <span className="rounded-full bg-[var(--color-surface-panel)] px-3 py-2 text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-accent)]">{reward.requiredPoints} pts</span>
                          </div>
                          <div className="mt-4 flex flex-wrap gap-2 text-xs font-semibold uppercase tracking-[0.16em] text-[var(--color-text-secondary)]">
                            <span>{reward.isSelectable ? copy.redeemableLabel : copy.lockedLabel}</span>
                            <span>{reward.requiresConfirmation ? copy.needsConfirmationLabel : copy.instantLabel}</span>
                            <span>{reward.isActive ? copy.activeLabel : copy.inactiveLabel}</span>
                          </div>
                        </article>
                      ))}
                    </div>
                  ) : <p className="mt-5 text-sm leading-7 text-[var(--color-text-secondary)]">{copy.noPublishedRewardsMessage}</p>}
                </article>

                <article id="promotions" className="rounded-[2rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-6 shadow-[var(--shadow-panel)]">
                  <div className="flex flex-wrap items-start justify-between gap-4">
                    <div>
                      <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-brand)]">{copy.promotionsTitle}</p>
                      <p className="mt-2 text-sm leading-7 text-[var(--color-text-secondary)]">{copy.promotionsDescription}</p>
                    </div>
                    {promotions ? <p className="rounded-full bg-[var(--color-surface-panel-strong)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)]">{formatResource(copy.visibleLabel, { count: promotions.diagnostics.finalCount })}</p> : null}
                  </div>
                  {promotions?.items.length ? (
                    <div className="mt-5 grid gap-4 md:grid-cols-2">
                      {promotions.items.map((item) => (
                        <article key={`${item.businessId}-${item.title}-${item.ctaKind}`} className="rounded-[1.5rem] bg-[var(--color-surface-panel-strong)] px-4 py-4">
                          <div className="flex items-start justify-between gap-4">
                            <div>
                              <p className="text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-accent)]">{item.campaignState}</p>
                              <h3 className="mt-2 text-lg font-semibold text-[var(--color-text-primary)]">{item.title}</h3>
                            </div>
                            <span className="rounded-full bg-[var(--color-surface-panel)] px-3 py-2 text-xs font-semibold uppercase tracking-[0.16em] text-[var(--color-text-primary)]">P{item.priority}</span>
                          </div>
                          <p className="mt-3 text-sm leading-7 text-[var(--color-text-secondary)]">{item.description}</p>
                          {(item.startsAtUtc || item.endsAtUtc) ? <div className="mt-3 text-sm leading-7 text-[var(--color-text-secondary)]">{item.startsAtUtc ? <p>{formatResource(copy.startsLabel, { value: formatDateTime(item.startsAtUtc, culture) })}</p> : null}{item.endsAtUtc ? <p>{formatResource(copy.endsLabel, { value: formatDateTime(item.endsAtUtc, culture) })}</p> : null}</div> : null}
                          {item.eligibilityRules.length > 0 ? <div className="mt-3 flex flex-wrap gap-2">{item.eligibilityRules.slice(0, 3).map((rule, index) => <span key={`${item.title}-${rule.audienceKind}-${index}`} className="rounded-full border border-[var(--color-border-soft)] px-3 py-2 text-xs font-semibold uppercase tracking-[0.14em] text-[var(--color-text-secondary)]">{rule.tierKey ?? rule.audienceKind}</span>)}</div> : null}
                          <form action={trackMemberPromotionInteractionAction} className="mt-5">
                            <input type="hidden" name="businessId" value={item.businessId} />
                            <input type="hidden" name="businessName" value={item.businessName} />
                            <input type="hidden" name="title" value={item.title} />
                            <input type="hidden" name="ctaKind" value={item.ctaKind} />
                            <input type="hidden" name="eventType" value={getPromotionEventType(item)} />
                            <input type="hidden" name="returnPath" value={localizeHref(`/loyalty/${businessId}`, culture)} />
                            <button type="submit" className="inline-flex rounded-full border border-[var(--color-border-soft)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel)]">{getPromotionLabel(item, copy)}</button>
                          </form>
                        </article>
                      ))}
                    </div>
                  ) : <p className="mt-5 text-sm leading-7 text-[var(--color-text-secondary)]">{copy.noPromotionCardsMessage}</p>}
                </article>

                <article className="rounded-[2rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-6 shadow-[var(--shadow-panel)]">
                  <div className="flex flex-wrap items-start justify-between gap-4">
                    <div>
                      <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-brand)]">{copy.unifiedTimelineTitle}</p>
                      <p className="mt-2 text-sm leading-7 text-[var(--color-text-secondary)]">{copy.unifiedTimelineDescription}</p>
                    </div>
                    {loadMoreHref ? <Link href={loadMoreHref} className="inline-flex rounded-full border border-[var(--color-border-soft)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel-strong)]">{copy.olderActivityCta}</Link> : null}
                  </div>
                  {timeline?.items.length ? (
                    <div className="mt-5 flex flex-col gap-4">
                      {timeline.items.map((entry) => (
                        <article key={`${entry.id}-${entry.occurredAtUtc}`} className="rounded-[1.5rem] bg-[var(--color-surface-panel-strong)] px-4 py-4">
                          <div className="flex flex-wrap items-start justify-between gap-4">
                            <div>
                              <p className="text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-accent)]">{getTimelineKind(entry, copy)}</p>
                              <p className="mt-2 text-sm leading-7 text-[var(--color-text-secondary)]">{entry.note ?? entry.reference ?? copy.noAdditionalNoteProvided}</p>
                              <p className="mt-1 text-sm leading-7 text-[var(--color-text-secondary)]">{formatDateTime(entry.occurredAtUtc, culture)}</p>
                            </div>
                            <p className="text-sm font-semibold text-[var(--color-text-primary)]">{getTimelineValue(entry, copy)}</p>
                          </div>
                        </article>
                      ))}
                    </div>
                  ) : <p className="mt-5 text-sm leading-7 text-[var(--color-text-secondary)]">{copy.noTimelineEntriesMessage}</p>}
                </article>
              </div>

              <div className="flex flex-col gap-5">
                <aside className="rounded-[2rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-6 shadow-[var(--shadow-panel)]">
                  <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-accent)]">{copy.accountDetailsTitle}</p>
                  <div className="mt-5 space-y-3 text-sm leading-7 text-[var(--color-text-secondary)]">
                    <div className="flex items-center justify-between gap-4"><span>{copy.statusLabel}</span><span>{dashboard.account.status}</span></div>
                    <div className="flex items-center justify-between gap-4"><span>{copy.loyaltyAccountLabel}</span><span className="truncate">{dashboard.account.loyaltyAccountId}</span></div>
                    <div className="flex items-center justify-between gap-4"><span>{copy.businessIdShortLabel}</span><span className="truncate">{dashboard.account.businessId}</span></div>
                  </div>
                </aside>
                <aside className="rounded-[2rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-6 shadow-[var(--shadow-panel)]">
                  <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-accent)]">{copy.recentTransactionsTitle}</p>
                  {dashboard.recentTransactions.length ? <div className="mt-5 flex flex-col gap-4">{dashboard.recentTransactions.map((transaction) => <article key={`${transaction.occurredAtUtc}-${transaction.type}-${transaction.delta}`} className="rounded-[1.5rem] bg-[var(--color-surface-panel-strong)] px-4 py-4"><div className="flex items-start justify-between gap-4"><div><p className="text-sm font-semibold text-[var(--color-text-primary)]">{transaction.type}</p><p className="mt-1 text-sm leading-7 text-[var(--color-text-secondary)]">{formatDateTime(transaction.occurredAtUtc, culture)}</p>{transaction.notes ? <p className="mt-1 text-sm leading-7 text-[var(--color-text-secondary)]">{transaction.notes}</p> : null}</div><p className="text-sm font-semibold text-[var(--color-text-primary)]">{transaction.delta >= 0 ? "+" : ""}{transaction.delta} pts</p></div></article>)}</div> : <p className="mt-5 text-sm leading-7 text-[var(--color-text-secondary)]">{copy.noRecentTransactionsMessage}</p>}
                </aside>
                {promotions ? <aside className="rounded-[2rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-6 shadow-[var(--shadow-panel)]"><p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-accent)]">{copy.feedDiagnosticsTitle}</p><div className="mt-5 space-y-3 text-sm leading-7 text-[var(--color-text-secondary)]"><div className="flex items-center justify-between gap-4"><span>{copy.initialCandidatesLabel}</span><span>{promotions.diagnostics.initialCandidates}</span></div><div className="flex items-center justify-between gap-4"><span>{copy.suppressedLabel}</span><span>{promotions.diagnostics.suppressedByFrequency}</span></div><div className="flex items-center justify-between gap-4"><span>{copy.deduplicatedLabel}</span><span>{promotions.diagnostics.deduplicated}</span></div><div className="flex items-center justify-between gap-4"><span>{copy.trimmedByCapLabel}</span><span>{promotions.diagnostics.trimmedByCap}</span></div></div></aside> : null}
              </div>
            </div>
          </>
        )}
      </div>
    </section>
  );
}
