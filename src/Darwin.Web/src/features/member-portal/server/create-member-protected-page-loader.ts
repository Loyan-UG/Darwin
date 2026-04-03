import "server-only";
import { getMemberEntryContext } from "@/features/member-portal/server/get-member-entry-context";
import { createMemberProtectedPageLoaderCore } from "@/features/member-portal/server/create-member-protected-page-loader-core";

type MemberEntryContext = Awaited<ReturnType<typeof getMemberEntryContext>>;

type CreateMemberProtectedPageLoaderOptions<
  TArgs extends [string, ...unknown[]],
  TRouteContext,
> = {
  operation: string;
  thresholdMs?: number;
  getContext: (...args: TArgs) => Record<string, unknown>;
  getEntryRoute: (...args: TArgs) => string;
  summarizeAuthorized: (
    routeContext: TRouteContext,
    result: {
      entryContext: MemberEntryContext;
      routeContext: TRouteContext | null;
    },
  ) => Record<string, unknown>;
  loadEntryContext?: (
    culture: string,
    route: string,
  ) => Promise<MemberEntryContext>;
  loadRouteContext: (...args: TArgs) => Promise<TRouteContext>;
};

export function createMemberProtectedPageLoader<
  TArgs extends [string, ...unknown[]],
  TRouteContext,
>({
  operation,
  thresholdMs = 300,
  getContext,
  getEntryRoute,
  summarizeAuthorized,
  loadEntryContext = getMemberEntryContext,
  loadRouteContext,
}: CreateMemberProtectedPageLoaderOptions<TArgs, TRouteContext>) {
  return createMemberProtectedPageLoaderCore({
    operation,
    thresholdMs,
    getContext,
    getEntryRoute,
    summarizeAuthorized,
    loadEntryContext,
    loadRouteContext,
  });
}
