import Link from "next/link";
import { MemberPortalNav } from "@/components/account/member-portal-nav";
import { StatusBanner } from "@/components/feedback/status-banner";
import { MemberCrossSurfaceRail } from "@/components/member/member-cross-surface-rail";
import type {
  PublicCategorySummary,
  PublicProductSummary,
} from "@/features/catalog/types";
import type { PublicPageSummary } from "@/features/cms/types";
import type { MemberInvoiceSummary } from "@/features/member-portal/types";
import { formatResource, getMemberResource } from "@/localization";
import { formatDateTime, formatMoney } from "@/lib/formatting";
import { buildAppQueryPath, localizeHref } from "@/lib/locale-routing";

type InvoicesPageProps = {
  culture: string;
  invoices: MemberInvoiceSummary[];
  status: string;
  currentPage: number;
  totalPages: number;
  cmsPages: PublicPageSummary[];
  cmsPagesStatus: string;
  categories: PublicCategorySummary[];
  categoriesStatus: string;
  products: PublicProductSummary[];
  productsStatus: string;
};

function buildInvoicesHref(page = 1) {
  return page > 1 ? `/invoices?page=${page}` : "/invoices";
}

export function InvoicesPage({
  culture,
  invoices,
  status,
  currentPage,
  totalPages,
  cmsPages,
  cmsPagesStatus,
  categories,
  categoriesStatus,
  products,
  productsStatus,
}: InvoicesPageProps) {
  const copy = getMemberResource(culture);
  const outstandingInvoices = invoices.filter((invoice) => invoice.balanceMinor > 0);
  const outstandingBalanceMinor = outstandingInvoices.reduce(
    (total, invoice) => total + invoice.balanceMinor,
    0,
  );
  const primaryCurrency = invoices[0]?.currency ?? "EUR";

  return (
    <section className="mx-auto flex w-full max-w-[var(--content-max-width)] flex-1 px-5 py-12 sm:px-6 lg:px-8">
      <div className="grid w-full gap-8 lg:grid-cols-[minmax(0,1fr)_320px]">
        <div className="flex flex-col gap-8">
        <nav
          aria-label={copy.memberBreadcrumbLabel}
          className="flex flex-wrap items-center gap-2 text-sm text-[var(--color-text-secondary)]"
        >
          <Link href={localizeHref("/", culture)} className="transition hover:text-[var(--color-brand)]">
            {copy.memberBreadcrumbHome}
          </Link>
          <span>/</span>
          <Link href={localizeHref("/account", culture)} className="transition hover:text-[var(--color-brand)]">
            {copy.memberBreadcrumbAccount}
          </Link>
          <span>/</span>
          <span className="font-medium text-[var(--color-text-primary)]">
            {copy.memberBreadcrumbInvoices}
          </span>
        </nav>

        <div className="rounded-[2rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-8 shadow-[var(--shadow-panel)] sm:px-8 sm:py-10">
          <p className="text-xs font-semibold uppercase tracking-[0.26em] text-[var(--color-brand)]">
            {copy.invoicesEyebrow}
          </p>
          <h1 className="mt-4 font-[family-name:var(--font-display)] text-4xl leading-tight text-[var(--color-text-primary)] sm:text-5xl">
            {copy.invoicesTitle}
          </h1>
        </div>

        {status !== "ok" && (
          <StatusBanner
            tone="warning"
            title={copy.invoicesWarningsTitle}
            message={formatResource(copy.invoicesWarningsMessage, { status })}
          />
        )}

        <div className="rounded-[2rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel-strong)] px-6 py-6 shadow-[var(--shadow-panel)]">
          <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-accent)]">
            {copy.invoicesWindowTitle}
          </p>
          <p className="mt-3 text-sm leading-7 text-[var(--color-text-secondary)]">
            {formatResource(copy.invoicesWindowMessage, {
              count: invoices.length,
              currentPage,
              totalPages,
              status,
            })}
          </p>
        </div>

        <div className="rounded-[2rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-6 shadow-[var(--shadow-panel)]">
          <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-accent)]">
            {copy.invoicesReadinessTitle}
          </p>
          <p className="mt-3 text-sm leading-7 text-[var(--color-text-secondary)]">
            {formatResource(copy.invoicesReadinessMessage, {
              count: outstandingInvoices.length,
              balance: formatMoney(outstandingBalanceMinor, primaryCurrency, culture),
            })}
          </p>
          <div className="mt-5 grid gap-3 sm:grid-cols-3">
            <div className="rounded-[1.5rem] bg-[var(--color-surface-panel-strong)] px-4 py-4">
              <p className="text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-text-secondary)]">
                {copy.invoicesReadinessVisibleLabel}
              </p>
              <p className="mt-3 text-base font-semibold text-[var(--color-text-primary)]">
                {invoices.length}
              </p>
            </div>
            <div className="rounded-[1.5rem] bg-[var(--color-surface-panel-strong)] px-4 py-4">
              <p className="text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-text-secondary)]">
                {copy.invoicesReadinessOutstandingLabel}
              </p>
              <p className="mt-3 text-base font-semibold text-[var(--color-text-primary)]">
                {outstandingInvoices.length}
              </p>
            </div>
            <div className="rounded-[1.5rem] bg-[var(--color-surface-panel-strong)] px-4 py-4">
              <p className="text-xs font-semibold uppercase tracking-[0.18em] text-[var(--color-text-secondary)]">
                {copy.invoicesReadinessBalanceLabel}
              </p>
              <p className="mt-3 text-base font-semibold text-[var(--color-text-primary)]">
                {formatMoney(outstandingBalanceMinor, primaryCurrency, culture)}
              </p>
            </div>
          </div>
          <div className="mt-5 flex flex-wrap gap-3">
            {outstandingInvoices[0] ? (
              <Link
                href={localizeHref(`/invoices/${outstandingInvoices[0].id}`, culture)}
                className="inline-flex rounded-full border border-[var(--color-border-soft)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel-strong)]"
              >
                {copy.invoicesReadinessPrimaryCta}
              </Link>
            ) : null}
            <Link
              href={localizeHref("/account", culture)}
              className="inline-flex rounded-full border border-[var(--color-border-soft)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel-strong)]"
            >
              {copy.securityBackToDashboardCta}
            </Link>
          </div>
        </div>

        <div className="rounded-[2rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-6 py-6 shadow-[var(--shadow-panel)]">
          <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-brand)]">
            {copy.invoicesStorefrontWindowTitle}
          </p>
          <p className="mt-3 text-sm leading-7 text-[var(--color-text-secondary)]">
            {formatResource(copy.invoicesStorefrontWindowMessage, {
              cmsStatus: cmsPagesStatus,
              categoriesStatus,
              productsStatus,
              pageCount: cmsPages.length,
              categoryCount: categories.length,
              productCount: products.length,
            })}
          </p>
          <div className="mt-5 grid gap-4 xl:grid-cols-3">
            <div className="rounded-[1.5rem] bg-[var(--color-surface-panel-strong)] px-4 py-4">
              <div className="flex items-center justify-between gap-3">
                <p className="text-sm font-semibold text-[var(--color-text-primary)]">
                  {copy.invoicesStorefrontCmsTitle}
                </p>
                <Link
                  href={localizeHref("/cms", culture)}
                  className="text-sm font-semibold text-[var(--color-brand)] transition hover:text-[var(--color-brand-strong)]"
                >
                  {copy.invoicesStorefrontCmsCta}
                </Link>
              </div>
              {cmsPages.length > 0 ? (
                <div className="mt-4 flex flex-col gap-3">
                  {cmsPages.map((page) => (
                    <Link
                      key={page.id}
                      href={localizeHref(`/cms/${page.slug}`, culture)}
                      className="rounded-2xl border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-4 py-3 transition hover:bg-[var(--color-surface-panel-strong)]"
                    >
                      <p className="font-semibold text-[var(--color-text-primary)]">
                        {page.title}
                      </p>
                      <p className="mt-2 text-sm leading-7 text-[var(--color-text-secondary)]">
                        {page.metaDescription ?? copy.invoicesStorefrontCmsFallbackDescription}
                      </p>
                    </Link>
                  ))}
                </div>
              ) : (
                <p className="mt-4 text-sm leading-7 text-[var(--color-text-secondary)]">
                  {formatResource(copy.invoicesStorefrontCmsEmptyMessage, {
                    status: cmsPagesStatus,
                  })}
                </p>
              )}
            </div>

            <div className="rounded-[1.5rem] bg-[var(--color-surface-panel-strong)] px-4 py-4">
              <div className="flex items-center justify-between gap-3">
                <p className="text-sm font-semibold text-[var(--color-text-primary)]">
                  {copy.invoicesStorefrontCatalogTitle}
                </p>
                <Link
                  href={localizeHref("/catalog", culture)}
                  className="text-sm font-semibold text-[var(--color-brand)] transition hover:text-[var(--color-brand-strong)]"
                >
                  {copy.invoicesStorefrontCatalogCta}
                </Link>
              </div>
              {categories.length > 0 ? (
                <div className="mt-4 flex flex-col gap-3">
                  {categories.map((category) => (
                    <Link
                      key={category.id}
                      href={localizeHref(
                        buildAppQueryPath("/catalog", {
                          category: category.slug,
                        }),
                        culture,
                      )}
                      className="rounded-2xl border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-4 py-3 transition hover:bg-[var(--color-surface-panel-strong)]"
                    >
                      <p className="font-semibold text-[var(--color-text-primary)]">
                        {category.name}
                      </p>
                      <p className="mt-2 text-sm leading-7 text-[var(--color-text-secondary)]">
                        {category.description ?? copy.invoicesStorefrontCatalogFallbackDescription}
                      </p>
                    </Link>
                  ))}
                </div>
              ) : (
                <p className="mt-4 text-sm leading-7 text-[var(--color-text-secondary)]">
                  {formatResource(copy.invoicesStorefrontCatalogEmptyMessage, {
                    status: categoriesStatus,
                  })}
                </p>
              )}
            </div>

            <div className="rounded-[1.5rem] bg-[var(--color-surface-panel-strong)] px-4 py-4">
              <div className="flex items-center justify-between gap-3">
                <p className="text-sm font-semibold text-[var(--color-text-primary)]">
                  {copy.invoicesStorefrontProductTitle}
                </p>
                <Link
                  href={localizeHref("/catalog", culture)}
                  className="text-sm font-semibold text-[var(--color-brand)] transition hover:text-[var(--color-brand-strong)]"
                >
                  {copy.invoicesStorefrontProductCta}
                </Link>
              </div>
              {products.length > 0 ? (
                <div className="mt-4 flex flex-col gap-3">
                  {products.map((product) => (
                    <Link
                      key={product.id}
                      href={localizeHref(`/catalog/${product.slug}`, culture)}
                      className="rounded-2xl border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] px-4 py-3 transition hover:bg-[var(--color-surface-panel-strong)]"
                    >
                      <p className="font-semibold text-[var(--color-text-primary)]">
                        {product.name}
                      </p>
                      <p className="mt-2 text-sm leading-7 text-[var(--color-text-secondary)]">
                        {product.shortDescription ?? copy.invoicesStorefrontProductFallbackDescription}
                      </p>
                      <p className="mt-2 font-semibold text-[var(--color-text-primary)]">
                        {formatMoney(product.priceMinor, product.currency, culture)}
                      </p>
                    </Link>
                  ))}
                </div>
              ) : (
                <p className="mt-4 text-sm leading-7 text-[var(--color-text-secondary)]">
                  {formatResource(copy.invoicesStorefrontProductEmptyMessage, {
                    status: productsStatus,
                  })}
                </p>
              )}
            </div>
          </div>
        </div>

        <div className="grid gap-5">
          {invoices.map((invoice) => (
            <article
              key={invoice.id}
              className="rounded-[2rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel)] p-6 shadow-[var(--shadow-panel)]"
            >
              <div className="flex flex-wrap items-start justify-between gap-4">
                <div>
                  <p className="text-xs font-semibold uppercase tracking-[0.22em] text-[var(--color-accent)]">
                    {invoice.status}
                  </p>
                  <h2 className="mt-3 text-2xl font-semibold text-[var(--color-text-primary)]">
                    <Link href={localizeHref(`/invoices/${invoice.id}`, culture)} className="transition hover:text-[var(--color-brand)]">
                      {invoice.orderNumber ?? invoice.id}
                    </Link>
                  </h2>
                  <p className="mt-2 text-sm leading-7 text-[var(--color-text-secondary)]">
                    {copy.createdLabel} {formatDateTime(invoice.createdAtUtc, culture)}
                  </p>
                </div>
                <div className="text-right">
                  <p className="text-lg font-semibold text-[var(--color-text-primary)]">
                    {formatMoney(invoice.totalGrossMinor, invoice.currency, culture)}
                  </p>
                  <p className="text-sm leading-7 text-[var(--color-text-secondary)]">
                    {formatResource(copy.balanceLabel, {
                      value: formatMoney(invoice.balanceMinor, invoice.currency, culture),
                    })}
                  </p>
                  <Link
                    href={localizeHref(`/invoices/${invoice.id}`, culture)}
                    className="mt-3 inline-flex rounded-full border border-[var(--color-border-soft)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel-strong)]"
                  >
                    {copy.openInvoiceCta}
                  </Link>
                </div>
              </div>
            </article>
          ))}
        </div>

        {invoices.length === 0 && (
          <div className="rounded-[2rem] border border-dashed border-[var(--color-border-strong)] bg-[var(--color-surface-panel)] px-6 py-10 text-center">
            <p className="text-sm leading-7 text-[var(--color-text-secondary)]">
              {copy.noInvoicesMessage}
            </p>
            <div className="mt-8 text-left">
              <MemberCrossSurfaceRail
                culture={culture}
                includeOrders
                includeInvoices={false}
                includeLoyalty
              />
            </div>
          </div>
        )}

        {totalPages > 1 && (
          <div className="flex flex-wrap items-center gap-3">
            <Link
              aria-disabled={currentPage <= 1}
              href={localizeHref(buildInvoicesHref(Math.max(1, currentPage - 1)), culture)}
              className="rounded-full border border-[var(--color-border-soft)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel-strong)] aria-[disabled=true]:pointer-events-none aria-[disabled=true]:opacity-40"
            >
              {copy.previous}
            </Link>
            <p className="text-sm text-[var(--color-text-secondary)]">
              {formatResource(copy.pageLabel, { currentPage, totalPages })}
            </p>
            <Link
              aria-disabled={currentPage >= totalPages}
              href={localizeHref(buildInvoicesHref(Math.min(totalPages, currentPage + 1)), culture)}
              className="rounded-full border border-[var(--color-border-soft)] px-4 py-2 text-sm font-semibold text-[var(--color-text-primary)] transition hover:bg-[var(--color-surface-panel-strong)] aria-[disabled=true]:pointer-events-none aria-[disabled=true]:opacity-40"
            >
              {copy.next}
            </Link>
          </div>
        )}

        </div>

        <div className="flex flex-col gap-6">
          <MemberPortalNav culture={culture} activePath="/invoices" />

          <aside className="rounded-[2rem] border border-[var(--color-border-soft)] bg-[var(--color-surface-panel-strong)] px-6 py-8 shadow-[var(--shadow-panel)] sm:px-8">
            <p className="text-xs font-semibold uppercase tracking-[0.26em] text-[var(--color-accent)]">
              {copy.invoicesRouteLabel}
            </p>
            <p className="mt-5 text-sm leading-7 text-[var(--color-text-secondary)]">
              {copy.invoicesPortalNote}
            </p>
          </aside>

          <MemberCrossSurfaceRail
            culture={culture}
            includeOrders
            includeInvoices={false}
            includeLoyalty
          />
        </div>
      </div>
    </section>
  );
}
