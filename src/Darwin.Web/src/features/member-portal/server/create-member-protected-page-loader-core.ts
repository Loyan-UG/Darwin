import { summarizePublicStorefrontHealth } from "@/lib/route-health";
import { createCachedObservedLoader } from "@/lib/observed-loader";
import {
  buildPageLoaderBaseDiagnostics,
  buildProtectedRouteFootprint,
} from "@/lib/page-loader-diagnostics";

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
  normalizeArgs?: (...args: TArgs) => TArgs;
  getContext: (...args: TArgs) => Record<string, unknown>;
  getEntryRoute: (...args: TArgs) => string;
  summarizeAuthorized: (
    routeContext: TRouteContext,
    result: ProtectedPageContext<TEntryContext, TRouteContext>,
  ) => Record<string, unknown>;
  loadEntryContext: (culture: string, route: string) => Promise<TEntryContext>;
  loadRouteContext: (...args: TArgs) => Promise<TRouteContext>;
};

export function buildMemberProtectedPageLoaderObservationContext(
  entryRoute: string,
  context?: Record<string, unknown>,
  options?: {
    hasCanonicalNormalization?: boolean;
  },
) {
  return buildPageLoaderBaseDiagnostics("member-protected", {
    hasCanonicalNormalization: options?.hasCanonicalNormalization,
    extras: {
      entryRoute,
      ...(context ?? {}),
    },
  });
}

export function buildMemberProtectedPageLoaderSuccessContext<
  TEntryContext extends {
    session: unknown | null;
    storefrontContext: Parameters<typeof summarizePublicStorefrontHealth>[0] | null;
  },
  TRouteContext,
>(
  entryRoute: string,
  result: ProtectedPageContext<TEntryContext, TRouteContext>,
  summarizeAuthorized: (
    routeContext: TRouteContext,
    result: ProtectedPageContext<TEntryContext, TRouteContext>,
  ) => Record<string, unknown>,
  options?: {
    hasCanonicalNormalization?: boolean;
  },
) {
  const authGate = result.entryContext.session
    ? "authorized"
    : "guest-fallback";
  const routeContextState = result.routeContext
    ? "loaded"
    : "guest-fallback";
  const storefrontFallbackState = result.entryContext.storefrontContext
    ? "present"
    : "missing";

  return buildPageLoaderBaseDiagnostics("member-protected", {
    hasCanonicalNormalization: options?.hasCanonicalNormalization,
    extras: {
      entryRoute,
      authGate,
      sessionState: result.entryContext.session ? "present" : "missing",
      routeContextState,
      storefrontFallbackState,
      protectedRouteFootprint: buildProtectedRouteFootprint({
        authGate,
        routeContextState,
        storefrontFallbackState,
      }),
      ...(result.routeContext
        ? summarizeAuthorized(result.routeContext, result)
        : summarizePublicStorefrontHealth(
            result.entryContext.storefrontContext!,
          )),
    },
  });
}

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
  normalizeArgs,
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
    normalizeArgs,
    getContext: (...args: TArgs) =>
      buildMemberProtectedPageLoaderObservationContext(
        getEntryRoute(...args),
        getContext(...args) ?? {},
        {
          hasCanonicalNormalization: Boolean(normalizeArgs),
        },
      ),
    getSuccessContext: (
      result: ProtectedPageContext<TEntryContext, TRouteContext>,
      ...args: TArgs
    ) =>
      buildMemberProtectedPageLoaderSuccessContext(
        getEntryRoute(...args),
        result,
        summarizeAuthorized,
        {
          hasCanonicalNormalization: Boolean(normalizeArgs),
        },
      ),
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
