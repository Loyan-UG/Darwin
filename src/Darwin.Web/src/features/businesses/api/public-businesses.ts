import "server-only";
import { postPublicJson, fetchPublicJson } from "@/lib/api/fetch-public-json";
import type { PagedResponse } from "@/features/member-portal/types";
import type {
  BusinessCategoryKind,
  BusinessDetail,
  BusinessSummary,
} from "@/features/businesses/types";

export async function getPublicBusinessesForDiscovery(input?: {
  page?: number;
  pageSize?: number;
  query?: string;
  city?: string;
  countryCode?: string;
  categoryKindKey?: string;
  hasActiveLoyaltyProgram?: boolean;
  latitude?: number;
  longitude?: number;
  radiusMeters?: number;
}) {
  return postPublicJson<PagedResponse<BusinessSummary>>(
    "/api/v1/public/businesses/list",
    "business-discovery",
    {
      page: input?.page ?? 1,
      pageSize: input?.pageSize ?? 8,
      query: input?.query || null,
      city: input?.city || null,
      countryCode: input?.countryCode || null,
      categoryKindKey: input?.categoryKindKey || null,
      hasActiveLoyaltyProgram: input?.hasActiveLoyaltyProgram ?? true,
      near:
        typeof input?.latitude === "number" && typeof input?.longitude === "number"
          ? {
              latitude: input.latitude,
              longitude: input.longitude,
            }
          : null,
      radiusMeters: input?.radiusMeters ?? null,
    },
  );
}

export async function getPublicBusinessCategoryKinds() {
  return fetchPublicJson<{ items: BusinessCategoryKind[] }>(
    "/api/v1/public/businesses/category-kinds",
    "business-category-kinds",
  );
}

export async function getPublicBusinessDetail(businessId: string) {
  return fetchPublicJson<BusinessDetail>(
    `/api/v1/public/businesses/${encodeURIComponent(businessId)}`,
    "business-detail",
  );
}
