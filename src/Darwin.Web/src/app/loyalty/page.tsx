import {
  getPublicBusinessCategoryKinds,
  getPublicBusinessesForDiscovery,
} from "@/features/businesses/api/public-businesses";
import { LoyaltyOverviewPage } from "@/components/member/loyalty-overview-page";
import {
  getCurrentMemberLoyaltyBusinesses,
  getCurrentMemberLoyaltyOverview,
} from "@/features/member-portal/api/member-portal";
import { getMemberSession } from "@/features/member-session/cookies";
import { getMemberResource } from "@/localization";
import { getRequestCulture } from "@/lib/request-culture";
import { buildNoIndexMetadata } from "@/lib/seo";

export async function generateMetadata() {
  const culture = await getRequestCulture();
  const copy = getMemberResource(culture);

  return buildNoIndexMetadata(
    culture,
    copy.loyaltyMetaTitle,
    copy.loyaltyMetaDescription,
    "/loyalty",
  );
}

type LoyaltyPageProps = {
  searchParams?: Promise<Record<string, string | string[] | undefined>>;
};

function readSearchParam(value: string | string[] | undefined) {
  return Array.isArray(value) ? value[0] : value;
}

function readNumericSearchParam(value: string | string[] | undefined) {
  const raw = readSearchParam(value);
  if (!raw) {
    return undefined;
  }

  const parsed = Number(raw);
  return Number.isFinite(parsed) ? parsed : undefined;
}

export default async function LoyaltyPage({ searchParams }: LoyaltyPageProps) {
  const culture = await getRequestCulture();
  const resolvedSearchParams = searchParams ? await searchParams : undefined;
  const session = await getMemberSession();
  const joinedPage = Number(readSearchParam(resolvedSearchParams?.joinedPage) ?? "1");
  const safeJoinedPage = Number.isFinite(joinedPage) && joinedPage > 0 ? joinedPage : 1;
  const discoverPage = Number(
    readSearchParam(resolvedSearchParams?.discoverPage) ?? "1",
  );
  const safeDiscoveryPage =
    Number.isFinite(discoverPage) && discoverPage > 0 ? discoverPage : 1;
  const query = readSearchParam(resolvedSearchParams?.query)?.trim() || undefined;
  const city = readSearchParam(resolvedSearchParams?.city)?.trim() || undefined;
  const countryCode =
    readSearchParam(resolvedSearchParams?.countryCode)?.trim() || undefined;
  const category =
    readSearchParam(resolvedSearchParams?.category)?.trim() || undefined;
  const latitude = readNumericSearchParam(resolvedSearchParams?.latitude);
  const longitude = readNumericSearchParam(resolvedSearchParams?.longitude);
  const radiusKm = readNumericSearchParam(resolvedSearchParams?.radiusKm);
  const hasValidLatitude =
    typeof latitude === "number" && latitude >= -90 && latitude <= 90;
  const hasValidLongitude =
    typeof longitude === "number" && longitude >= -180 && longitude <= 180;
  const safeRadiusKm =
    typeof radiusKm === "number" && radiusKm >= 1 && radiusKm <= 50
      ? radiusKm
      : undefined;

  const [overviewResult, businessesResult, discoveryResult, categoryKindsResult] =
    await Promise.all([
      session
        ? getCurrentMemberLoyaltyOverview()
        : Promise.resolve({ data: null, status: "unauthenticated" as const }),
      session
        ? getCurrentMemberLoyaltyBusinesses({
            page: safeJoinedPage,
            pageSize: 8,
          })
        : Promise.resolve({ data: null, status: "unauthenticated" as const }),
      getPublicBusinessesForDiscovery({
        page: safeDiscoveryPage,
        pageSize: 6,
        query,
        city,
        countryCode,
        categoryKindKey: category,
        hasActiveLoyaltyProgram: true,
        latitude: hasValidLatitude ? latitude : undefined,
        longitude: hasValidLongitude ? longitude : undefined,
        radiusMeters:
          hasValidLatitude && hasValidLongitude && safeRadiusKm
            ? safeRadiusKm * 1000
            : undefined,
      }),
      getPublicBusinessCategoryKinds(),
    ]);

  return (
    <LoyaltyOverviewPage
      culture={culture}
      overview={overviewResult.data}
      status={overviewResult.status}
      businesses={businessesResult.data?.items ?? []}
      businessesStatus={businessesResult.status}
      currentPage={safeJoinedPage}
      totalPages={Math.max(
        1,
        Math.ceil(
          (businessesResult.data?.total ?? 0) /
            (businessesResult.data?.request.pageSize ?? 8),
        ),
      )}
      discoveryBusinesses={discoveryResult.data?.items ?? []}
      discoveryStatus={discoveryResult.status}
      discoveryCurrentPage={safeDiscoveryPage}
      discoveryTotalPages={Math.max(
        1,
        Math.ceil(
          (discoveryResult.data?.total ?? 0) /
            (discoveryResult.data?.request.pageSize ?? 6),
        ),
      )}
      discoveryQuery={query}
      discoveryCity={city}
      discoveryCountryCode={countryCode}
      discoveryCategory={category}
      discoveryLatitude={hasValidLatitude ? latitude : undefined}
      discoveryLongitude={hasValidLongitude ? longitude : undefined}
      discoveryRadiusKm={safeRadiusKm}
      discoveryCategories={categoryKindsResult.data?.items ?? []}
      hasMemberSession={Boolean(session)}
    />
  );
}
