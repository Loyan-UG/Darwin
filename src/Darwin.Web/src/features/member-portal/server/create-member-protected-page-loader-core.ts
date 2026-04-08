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
      buildPageLoaderBaseDiagnostics("member-protected", {
        hasCanonicalNormalization: Boolean(normalizeArgs),
        extras: {
          entryRoute: getEntryRoute(...args),
          ...(getContext(...args) ?? {}),
        },
      }),
    getSuccessContext: (
      result: ProtectedPageContext<TEntryContext, TRouteContext>,
      ...args: TArgs
    ) => {
      void args;
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
        hasCanonicalNormalization: Boolean(normalizeArgs),
        extras: {
          entryRoute: getEntryRoute(...args),
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
