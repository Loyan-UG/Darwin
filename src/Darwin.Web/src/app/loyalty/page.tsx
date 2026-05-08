import {
  getPublicBusinessCategoryKinds,
  getPublicBusinessesForDiscovery,
} from "@/features/businesses/api/public-businesses";
import { LoyaltyOverviewPage } from "@/components/member/loyalty-overview-page";
import {
  readBoundedNumericSearchParam,
  readPositiveIntegerSearchParam,
  readSearchTextParam,
  readUppercaseSearchTextParam,
} from "@/features/checkout/helpers";
import {
  getCurrentMemberLoyaltyBusinesses,
  getCurrentMemberLoyaltyOverviewForCulture,
} from "@/features/member-portal/api/member-portal";
import { getLoyaltyOverviewSeoMetadata } from "@/features/member-portal/server/get-loyalty-seo-metadata";
import { getMemberSession } from "@/features/member-session/cookies";
import { getRequestCulture } from "@/lib/request-culture";

export async function generateMetadata() {
  const culture = await getRequestCulture();
  const { metadata } = await getLoyaltyOverviewSeoMetadata(culture);
  return metadata;
}

type LoyaltyPageProps = {
  searchParams?: Promise<Record<string, string | string[] | undefined>>;
};

export default async function LoyaltyPage({ searchParams }: LoyaltyPageProps) {
  const culture = await getRequestCulture();
  const resolvedSearchParams = searchParams ? await searchParams : undefined;
  const session = await getMemberSession();
  const safeJoinedPage = readPositiveIntegerSearchParam(
    resolvedSearchParams?.joinedPage,
  );
  const safeDiscoveryPage = readPositiveIntegerSearchParam(
    resolvedSearchParams?.discoverPage,
  );
  const query = readSearchTextParam(resolvedSearchParams?.query, 80);
  const city = readSearchTextParam(resolvedSearchParams?.city, 80);
  const countryCode = readUppercaseSearchTextParam(
    resolvedSearchParams?.countryCode,
    2,
  );
  const category = readSearchTextParam(resolvedSearchParams?.category, 80);
  const latitude = readBoundedNumericSearchParam(resolvedSearchParams?.latitude, {
    min: -90,
    max: 90,
  });
  const longitude = readBoundedNumericSearchParam(
    resolvedSearchParams?.longitude,
    {
      min: -180,
      max: 180,
    },
  );
  const safeRadiusKm = readBoundedNumericSearchParam(
    resolvedSearchParams?.radiusKm,
    {
      min: 1,
      max: 50,
    },
  );

  const [overviewResult, businessesResult, discoveryResult, categoryKindsResult] =
    await Promise.all([
      session
        ? getCurrentMemberLoyaltyOverviewForCulture(culture)
        : Promise.resolve({ data: null, status: "unauthenticated" as const }),
      session
        ? getCurrentMemberLoyaltyBusinesses({
            page: safeJoinedPage,
            pageSize: 8,
            culture,
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
        latitude,
        longitude,
        radiusMeters:
          typeof latitude === "number" &&
          typeof longitude === "number" &&
          typeof safeRadiusKm === "number"
            ? safeRadiusKm * 1000
            : undefined,
        culture,
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
      discoveryLatitude={latitude}
      discoveryLongitude={longitude}
      discoveryRadiusKm={safeRadiusKm}
      discoveryCategories={categoryKindsResult.data?.items ?? []}
      hasMemberSession={Boolean(session)}
    />
  );
}
