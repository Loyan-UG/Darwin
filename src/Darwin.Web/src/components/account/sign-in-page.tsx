import { ActivationRecoveryPanel } from "@/components/account/activation-recovery-panel";
import { PublicAuthCompositionWindow } from "@/components/account/public-auth-composition-window";
import { PublicAuthReturnSummary } from "@/components/account/public-auth-return-summary";
import Link from "next/link";
import { PublicAuthContinuation } from "@/components/account/public-auth-continuation";
import type {
  PublicCategorySummary,
  PublicProductSummary,
} from "@/features/catalog/types";
import type { PublicCartSummary } from "@/features/cart/types";
import type { PublicPageSummary } from "@/features/cms/types";
import { StatusBanner } from "@/components/feedback/status-banner";
import { signInMemberAction } from "@/features/member-session/actions";
import { buildLocalizedAuthHref } from "@/lib/locale-routing";
import { formatResource, getMemberResource, resolveLocalizedQueryMessage } from "@/localization";

type SignInPageProps = {
  culture: string;
  email?: string;
  signInError?: string;
  returnPath?: string;
  cmsPages: PublicPageSummary[];
  cmsPagesStatus: string;
  categories: PublicCategorySummary[];
  categoriesStatus: string;
  products: PublicProductSummary[];
  productsStatus: string;
  storefrontCart: PublicCartSummary | null;
  storefrontCartStatus: string;
};

export function SignInPage({
  culture,
  email,
  signInError,
  returnPath,
  cmsPages,
  cmsPagesStatus,
  categories,
  categoriesStatus,
  products,
  productsStatus,
  storefrontCart,
  storefrontCartStatus,
}: SignInPageProps) {
  const copy = getMemberResource(culture);
  const resolvedSignInError = resolveLocalizedQueryMessage(signInError, copy);
  const registerHref = buildLocalizedAuthHref("/account/register", returnPath, culture);
  const activationHref = buildLocalizedAuthHref("/account/activation", returnPath, culture);
  const passwordHref = buildLocalizedAuthHref("/account/password", returnPath, culture);

  return (
    <section className="mx-auto flex w-full max-w-[var(--content-max-width)] flex-1 px-5 py-12 sm:px-6 lg:px-8">
      <div className="grid w-full gap-6 lg:grid-cols-[minmax(0,1fr)_320px]">
        <form
          action={signInMemberAction}
          className="rounded-[1rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-8 shadow-[var(--shadow-panel)] sm:px-8"
        >
          <input type="hidden" name="returnPath" value={returnPath || "/account"} />
          <p className="text-xs font-semibold uppercase tracking-[0.26em] text-[var(--color-brand)]">
            {copy.signInEyebrow}
          </p>
          <h1 className="mt-4 font-[family-name:var(--font-display)] text-4xl leading-tight text-[var(--color-text-primary)] sm:text-5xl">
            {copy.signInTitle}
          </h1>
          <p className="mt-5 max-w-2xl text-base leading-8 text-[var(--color-text-secondary)] sm:text-lg">
            {copy.signInDescription}
          </p>

          {resolvedSignInError && (
            <div className="mt-6">
              <StatusBanner
                tone="warning"
                title={copy.signInFailedTitle}
                message={resolvedSignInError}
              />
            </div>
          )}

          <div className="mt-8 grid gap-4">
            <label className="flex flex-col gap-2 text-sm font-medium text-[var(--color-text-primary)]">
              {copy.emailLabel}
              <input name="email" type="email" required autoComplete="email" inputMode="email" defaultValue={email} className="rounded-2xl border border-[var(--color-border-soft)] bg-[var(--color-surface-panel-strong)] px-4 py-3 text-sm font-normal outline-none" />
            </label>
            <label className="flex flex-col gap-2 text-sm font-medium text-[var(--color-text-primary)]">
              {copy.passwordLabel}
              <input name="password" type="password" required autoComplete="current-password" className="rounded-2xl border border-[var(--color-border-soft)] bg-[var(--color-surface-panel-strong)] px-4 py-3 text-sm font-normal outline-none" />
            </label>
          </div>

          <button
            type="submit"
            className="mt-8 inline-flex rounded-full bg-[var(--color-brand)] px-5 py-3 text-sm font-semibold text-[var(--color-brand-contrast)] transition hover:bg-[var(--color-brand-strong)]"
          >
            {copy.signInCta}
          </button>

          <div className="mt-6 flex flex-wrap gap-3">
            <Link
              href={registerHref}
              className="inline-flex rounded-full border border-[var(--color-border-soft)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel-strong)]"
            >
              {copy.createAccount}
            </Link>
            <Link
              href={activationHref}
              className="inline-flex rounded-full border border-[var(--color-border-soft)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel-strong)]"
            >
              {copy.activationFlowCta}
            </Link>
            <Link
              href={passwordHref}
              className="inline-flex rounded-full border border-[var(--color-border-soft)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel-strong)]"
            >
              {copy.cardPasswordCta}
            </Link>
          </div>
        </form>

        <div className="flex flex-col gap-6">
          <aside className="rounded-[1rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel-strong)] px-6 py-8 shadow-[var(--shadow-panel)] sm:px-8">
            <p className="text-xs font-semibold uppercase tracking-[0.26em] text-[var(--color-accent)]">
              {copy.sessionNoteTitle}
            </p>
            <ul className="mt-5 space-y-4 text-sm leading-7 text-[var(--color-text-secondary)]">
              <li className="rounded-2xl border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-4 py-3">
                {copy.sessionNoteArchitecture}
              </li>
              <li className="rounded-2xl border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-4 py-3">
                {copy.sessionNoteApi}
              </li>
            </ul>
          </aside>
          <PublicAuthReturnSummary
            culture={culture}
            returnPath={returnPath}
            storefrontCart={storefrontCart}
          />
          <ActivationRecoveryPanel
            culture={culture}
            email={email}
            returnPath={returnPath}
            compact
          />
          <PublicAuthCompositionWindow
            culture={culture}
            routeCard={{
              label: copy.publicAuthCompositionJourneyCurrentLabel,
              title: copy.publicAuthCompositionJourneySignInTitle,
              description: formatResource(copy.publicAuthCompositionJourneySignInDescription, {
                returnPath: returnPath || "/account",
              }),
              href: "/account/sign-in",
              ctaLabel: copy.publicAuthCompositionJourneyCurrentCta,
            }}
            nextCard={{
              label: copy.publicAuthCompositionJourneyNextLabel,
              title: copy.publicAuthCompositionJourneyRegisterTitle,
              description: copy.publicAuthCompositionJourneySignInNextDescription,
              href: registerHref,
              ctaLabel: copy.publicAuthCompositionJourneyRegisterCta,
            }}
            routeMapItems={[
              {
                label: copy.publicAuthCompositionRouteMapCurrentLabel,
                title: copy.publicAuthCompositionRouteMapSignInTitle,
                description: copy.publicAuthCompositionRouteMapSignInDescription,
                href: "/account/sign-in",
                ctaLabel: copy.publicAuthCompositionRouteMapCurrentCta,
              },
              {
                label: copy.publicAuthCompositionRouteMapNextLabel,
                title: copy.publicAuthCompositionRouteMapRegisterTitle,
                description: copy.publicAuthCompositionRouteMapRegisterDescription,
                href: registerHref,
                ctaLabel: copy.publicAuthCompositionRouteMapRegisterCta,
              },
            ]}
            cmsPages={cmsPages}
            categories={categories}
            products={products}
          />
          <PublicAuthContinuation
            culture={culture}
            cmsPages={cmsPages}
            cmsPagesStatus={cmsPagesStatus}
            categories={categories}
            categoriesStatus={categoriesStatus}
            products={products}
            productsStatus={productsStatus}
            storefrontCart={storefrontCart}
            storefrontCartStatus={storefrontCartStatus}
          />
        </div>
      </div>
    </section>
  );
}
