import { LoyaltyPublicBusinessPage } from "@/components/member/loyalty-public-business-page";
import { LoyaltyBusinessPage } from "@/components/member/loyalty-business-page";
import { getPublicBusinessDetail } from "@/features/businesses/api/public-businesses";
import {
  getCurrentMemberBusinessWithMyAccount,
  getCurrentMemberLoyaltyBusinessDashboard,
  getCurrentMemberPromotions,
  getCurrentMemberLoyaltyRewards,
  getCurrentMemberLoyaltyTimeline,
} from "@/features/member-portal/api/member-portal";
import { createQrCodeDataUrl } from "@/features/member-portal/qr-code";
import { readPreparedMemberLoyaltyScanSession } from "@/features/member-portal/scan-session-cookie";
import { getLoyaltyBusinessSeoMetadata } from "@/features/member-portal/server/get-loyalty-seo-metadata";
import { getMemberSession } from "@/features/member-session/cookies";
import { getRequestCulture } from "@/lib/request-culture";

type LoyaltyBusinessRouteProps = {
  params: Promise<{
    businessId: string;
  }>;
  searchParams: Promise<{
    beforeAtUtc?: string;
    beforeId?: string;
    promotionStatus?: string;
    promotionError?: string;
    joinStatus?: string;
    joinError?: string;
    scanStatus?: string;
    scanError?: string;
  }>;
};

export async function generateMetadata({ params }: LoyaltyBusinessRouteProps) {
  const culture = await getRequestCulture();
  const { businessId } = await params;
  const { metadata } = await getLoyaltyBusinessSeoMetadata(culture, businessId);
  return metadata;
}

export default async function LoyaltyBusinessRoute({
  params,
  searchParams,
}: LoyaltyBusinessRouteProps) {
  const culture = await getRequestCulture();
  const session = await getMemberSession();
  const [{ businessId }, cursor] = await Promise.all([params, searchParams]);
  const hasValidCursor = Boolean(cursor.beforeAtUtc && cursor.beforeId);
  const publicBusinessResult = await getPublicBusinessDetail(businessId, culture);

  if (!session) {
    return (
      <LoyaltyPublicBusinessPage
        culture={culture}
        businessId={businessId}
        detail={publicBusinessResult.data}
        detailStatus={publicBusinessResult.status}
        isAuthenticated={false}
        joinStatus={cursor.joinStatus}
        joinError={cursor.joinError}
      />
    );
  }

  const memberBusinessResult = await getCurrentMemberBusinessWithMyAccount(
    businessId,
    culture,
  );
  const businessDetail =
    memberBusinessResult.data?.business ?? publicBusinessResult.data;

  if (
    memberBusinessResult.status !== "ok" ||
    !memberBusinessResult.data?.hasAccount
  ) {
    return (
      <LoyaltyPublicBusinessPage
        culture={culture}
        businessId={businessId}
        detail={businessDetail}
        detailStatus={
          memberBusinessResult.status === "ok"
            ? publicBusinessResult.status
            : memberBusinessResult.status
        }
        isAuthenticated={memberBusinessResult.status === "ok"}
        joinStatus={cursor.joinStatus}
        joinError={cursor.joinError}
      />
    );
  }

  const [dashboardResult, rewardsResult, timelineResult, promotionsResult, preparedScanSession] = await Promise.all([
    getCurrentMemberLoyaltyBusinessDashboard(businessId, culture),
    getCurrentMemberLoyaltyRewards(businessId, culture),
    getCurrentMemberLoyaltyTimeline({
      businessId,
      pageSize: 10,
      beforeAtUtc: hasValidCursor ? cursor.beforeAtUtc : undefined,
      beforeId: hasValidCursor ? cursor.beforeId : undefined,
      culture,
    }),
    getCurrentMemberPromotions({
      businessId,
      maxItems: 4,
      culture,
    }),
    readPreparedMemberLoyaltyScanSession(businessId),
  ]);
  const qrCodeDataUrl = preparedScanSession
    ? await createQrCodeDataUrl(preparedScanSession.scanSessionToken)
    : null;

  return (
    <LoyaltyBusinessPage
      culture={culture}
      businessId={businessId}
      businessLocations={businessDetail?.locations ?? []}
      dashboard={dashboardResult.data}
      dashboardStatus={dashboardResult.status}
      rewards={rewardsResult.data}
      rewardsStatus={rewardsResult.status}
      timeline={timelineResult.data}
      timelineStatus={timelineResult.status}
      promotions={promotionsResult.data}
      promotionsStatus={promotionsResult.status}
      preparedScanSession={preparedScanSession}
      qrCodeDataUrl={qrCodeDataUrl}
      scanStatus={cursor.scanStatus}
      scanError={cursor.scanError}
      promotionStatus={cursor.promotionStatus}
      promotionError={cursor.promotionError}
    />
  );
}
