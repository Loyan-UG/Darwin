import { summarizePublicStorefrontHealth } from "@/lib/route-health";
import { createCachedObservedLoader } from "@/lib/observed-loader";

type ProtectedPageContext<TEntryContext, TRouteContext> = {
  entryContext: TEntryContext;
  routeContext: TRouteContext | null;
};

type CreateMemberProtectedPageLoaderOptions<
  TArgs extends [string, ...unknown[]],
  TEntryContext extends {
    session: unknown | null;
    storefrontContext: Parameters<typeof summarizePublicStorefrontHealth>[0] | null;
  },
  TRouteContext,
> = {
  operation: string;
  thresholdMs?: number;
  getContext: (...args: TArgs) => Record<string, unknown>;
  getEntryRoute: (...args: TArgs) => string;
  summarizeAuthorized: (
    routeContext: TRouteContext,
    result: ProtectedPageContext<TEntryContext, TRouteContext>,
  ) => Record<string, unknown>;
  loadEntryContext: (culture: string, route: string) => Promise<TEntryContext>;
  loadRouteContext: (...args: TArgs) => Promise<TRouteContext>;
};

export function createMemberProtectedPageLoaderCore<
  TArgs extends [string, ...unknown[]],
  TEntryContext extends {
    session: unknown | null;
    storefrontContext: Parameters<typeof summarizePublicStorefrontHealth>[0] | null;
  },
  TRouteContext,
>({
  operation,
  thresholdMs = 300,
  getContext,
  getEntryRoute,
  summarizeAuthorized,
  loadEntryContext,
  loadRouteContext,
}: CreateMemberProtectedPageLoaderOptions<TArgs, TEntryContext, TRouteContext>) {
  return createCachedObservedLoader({
    area: "member-protected-page-context",
    operation,
    thresholdMs,
    getContext: (...args: TArgs) => ({
      pageLoaderKind: "member-protected",
      entryRoute: getEntryRoute(...args),
      ...(getContext(...args) ?? {}),
    }),
    getSuccessContext: (
      result: ProtectedPageContext<TEntryContext, TRouteContext>,
      ...args: TArgs
    ) => {
      void args;

      return {
        pageLoaderKind: "member-protected",
        entryRoute: getEntryRoute(...args),
        authGate: result.entryContext.session ? "authorized" : "guest-fallback",
        sessionState: result.entryContext.session ? "present" : "missing",
        ...(result.routeContext
          ? summarizeAuthorized(result.routeContext, result)
          : summarizePublicStorefrontHealth(
              result.entryContext.storefrontContext!,
            )),
      };
    },
    load: async (...args: TArgs) => {
      const [culture] = args;
      const entryContext = await loadEntryContext(culture, getEntryRoute(...args));
      const routeContext = entryContext.session
        ? await loadRouteContext(...args)
        : null;

      return {
        entryContext,
        routeContext,
      };
    },
  });
}
