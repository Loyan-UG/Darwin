import Link from "next/link";
import { getMemberResource } from "@/localization";
import { localizeHref } from "@/lib/locale-routing";

type MemberPortalNavProps = {
  culture: string;
  activePath: string;
};

const ROUTES = [
  { href: "/account", key: "accountOverviewRouteLabel" },
  { href: "/account/profile", key: "profileRouteLabel" },
  { href: "/account/preferences", key: "preferencesRouteLabel" },
  { href: "/account/security", key: "securityRouteLabel" },
  { href: "/account/addresses", key: "addressesRouteLabel" },
  { href: "/orders", key: "ordersRouteLabel" },
  { href: "/invoices", key: "invoicesRouteLabel" },
  { href: "/loyalty", key: "loyaltyRouteLabel" },
] as const;

export function MemberPortalNav({
  culture,
  activePath,
}: MemberPortalNavProps) {
  const copy = getMemberResource(culture);

  return (
    <aside className="rounded-[1rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-6 shadow-[var(--shadow-panel)]">
      <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-brand)]">
        {copy.portalRoutesTitle}
      </p>
      <p className="mt-3 text-sm leading-7 text-[var(--color-text-secondary)]">
        {copy.portalRoutesDescription}
      </p>
      <div className="mt-5 flex flex-col gap-2">
        {ROUTES.map((route) => {
          const isActive = activePath === route.href;
          return (
            <Link
              key={route.href}
              href={localizeHref(route.href, culture)}
              className={
                isActive
                  ? "rounded-2xl bg-[var(--color-brand)] px-4 py-3 text-sm font-semibold text-[var(--color-brand-contrast)]"
                  : "rounded-2xl border border-[var(--color-border-soft)] px-4 py-3 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel-strong)]"
              }
            >
              {copy[route.key]}
            </Link>
          );
        })}
      </div>
    </aside>
  );
}
