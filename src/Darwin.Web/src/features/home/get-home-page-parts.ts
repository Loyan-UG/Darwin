import "server-only";
import {
  getPublicCategories,
  getPublicProducts,
} from "@/features/catalog/api/public-catalog";
import { getPublishedPages } from "@/features/cms/api/public-cms";
import { formatMoney } from "@/lib/formatting";
import { getSupportedCultures } from "@/lib/request-culture";
import { formatResource, getHomeResource } from "@/localization";
import type { WebPagePart } from "@/web-parts/types";

export async function getHomePageParts(culture: string): Promise<WebPagePart[]> {
  const copy = getHomeResource(culture);
  const supportedCultures = getSupportedCultures();
  const [pagesResult, productsResult, categoriesResult] = await Promise.all([
    getPublishedPages({
      page: 1,
      pageSize: 3,
      culture,
    }),
    getPublicProducts({
      page: 1,
      pageSize: 3,
      culture,
    }),
    getPublicCategories(culture),
  ]);
  const cmsHealthy = pagesResult.status === "ok";
  const catalogHealthy = productsResult.status === "ok";
  const categoriesHealthy = categoriesResult.status === "ok";

  return [
    {
      id: "home-hero",
      kind: "hero",
      eyebrow: copy.heroEyebrow,
      title: copy.heroTitle,
      description: copy.heroDescription,
      actions: [
        { label: copy.browseCatalogCta, href: "/catalog" },
        { label: copy.openCheckoutCta, href: "/checkout", variant: "secondary" },
      ],
      highlights: [
        copy.heroHighlightTheme,
        copy.heroHighlightComposition,
        formatResource(copy.heroHighlightStatus, {
          pagesStatus: pagesResult.status,
          productsStatus: productsResult.status,
        }),
        formatResource(copy.heroHighlightCategoriesStatus, {
          categoriesStatus: categoriesResult.status,
        }),
      ],
      panelTitle: copy.heroPanelTitle,
    },
    {
      id: "home-metrics",
      kind: "stat-grid",
      eyebrow: copy.metricsEyebrow,
      title: copy.metricsTitle,
      description: copy.metricsDescription,
      metrics: [
        {
          id: "metric-pages",
          label: copy.metricPagesLabel,
          value: String(pagesResult.data?.total ?? pagesResult.data?.items.length ?? 0),
          note: cmsHealthy
            ? copy.metricPagesNote
            : formatResource(copy.metricPagesDegradedNote, {
                status: pagesResult.status,
              }),
        },
        {
          id: "metric-products",
          label: copy.metricProductsLabel,
          value: String(
            productsResult.data?.total ?? productsResult.data?.items.length ?? 0,
          ),
          note: catalogHealthy
            ? copy.metricProductsNote
            : formatResource(copy.metricProductsDegradedNote, {
                status: productsResult.status,
              }),
        },
        {
          id: "metric-categories",
          label: copy.metricCategoriesLabel,
          value: String(
            categoriesResult.data?.total ??
              categoriesResult.data?.items.length ??
              0,
          ),
          note: categoriesHealthy
            ? copy.metricCategoriesNote
            : formatResource(copy.metricCategoriesDegradedNote, {
                status: categoriesResult.status,
              }),
        },
        {
          id: "metric-cultures",
          label: copy.metricCulturesLabel,
          value: String(supportedCultures.length),
          note: copy.metricCulturesNote,
        },
      ],
    },
    {
      id: "home-contract-rail",
      kind: "status-list",
      eyebrow: copy.contractRailEyebrow,
      title: copy.contractRailTitle,
      description: copy.contractRailDescription,
      items: [
        {
          id: "contract-cms",
          label: copy.contractCmsLabel,
          title: copy.contractCmsTitle,
          description: cmsHealthy
            ? copy.contractCmsHealthyDescription
            : formatResource(copy.contractCmsDegradedDescription, {
                status: pagesResult.status,
              }),
          href: "/cms",
          ctaLabel: copy.openCmsCta,
          tone: cmsHealthy ? "ok" : "warning",
          meta: formatResource(copy.contractCmsMeta, {
            count: pagesResult.data?.items.length ?? 0,
          }),
        },
        {
          id: "contract-catalog",
          label: copy.contractCatalogLabel,
          title: copy.contractCatalogTitle,
          description: catalogHealthy
            ? copy.contractCatalogHealthyDescription
            : formatResource(copy.contractCatalogDegradedDescription, {
                status: productsResult.status,
              }),
          href: "/catalog",
          ctaLabel: copy.browseCatalogCta,
          tone: catalogHealthy ? "ok" : "warning",
          meta: formatResource(copy.contractCatalogMeta, {
            count: productsResult.data?.items.length ?? 0,
          }),
        },
        {
          id: "contract-account",
          label: copy.contractAccountLabel,
          title: copy.contractAccountTitle,
          description: copy.contractAccountDescription,
          href: "/account",
          ctaLabel: copy.openAccountCta,
          tone: "ok",
          meta: formatResource(copy.contractAccountMeta, {
            count: supportedCultures.length,
          }),
        },
      ],
      emptyMessage: copy.contractRailEmptyMessage,
    },
    {
      id: "home-stage-flow",
      kind: "stage-flow",
      eyebrow: copy.stageFlowEyebrow,
      title: copy.stageFlowTitle,
      description: copy.stageFlowDescription,
      items: [
        {
          id: "stage-content",
          step: copy.stageContentStep,
          title: copy.stageContentTitle,
          description: copy.stageContentDescription,
          href: "/cms",
          ctaLabel: copy.openCmsCta,
          meta: formatResource(copy.stageContentMeta, {
            status: pagesResult.status,
          }),
        },
        {
          id: "stage-discovery",
          step: copy.stageDiscoveryStep,
          title: copy.stageDiscoveryTitle,
          description: copy.stageDiscoveryDescription,
          href: "/catalog",
          ctaLabel: copy.browseCatalogCta,
          meta: formatResource(copy.stageDiscoveryMeta, {
            status: productsResult.status,
          }),
        },
        {
          id: "stage-follow-up",
          step: copy.stageFollowUpStep,
          title: copy.stageFollowUpTitle,
          description: copy.stageFollowUpDescription,
          href: "/account",
          ctaLabel: copy.openAccountCta,
          meta: copy.stageFollowUpMeta,
        },
      ],
      emptyMessage: copy.stageFlowEmptyMessage,
    },
    {
      id: "home-pair-panel",
      kind: "pair-panel",
      eyebrow: copy.pairPanelEyebrow,
      title: copy.pairPanelTitle,
      description: copy.pairPanelDescription,
      leading: {
        id: "pair-cms",
        eyebrow: copy.pairCmsEyebrow,
        title: copy.pairCmsTitle,
        description: cmsHealthy
          ? copy.pairCmsHealthyDescription
          : formatResource(copy.pairCmsDegradedDescription, {
              status: pagesResult.status,
            }),
        href: "/cms",
        ctaLabel: copy.openCmsCta,
        meta: formatResource(copy.pairCmsMeta, {
          count: pagesResult.data?.items.length ?? 0,
        }),
      },
      trailing: {
        id: "pair-catalog",
        eyebrow: copy.pairCatalogEyebrow,
        title: copy.pairCatalogTitle,
        description: catalogHealthy
          ? copy.pairCatalogHealthyDescription
          : formatResource(copy.pairCatalogDegradedDescription, {
              status: productsResult.status,
            }),
        href: "/catalog",
        ctaLabel: copy.browseCatalogCta,
        meta: formatResource(copy.pairCatalogMeta, {
          count: productsResult.data?.items.length ?? 0,
        }),
      },
    },
    {
      id: "home-agenda-columns",
      kind: "agenda-columns",
      eyebrow: copy.agendaEyebrow,
      title: copy.agendaTitle,
      description: copy.agendaDescription,
      columns: [
        {
          id: "agenda-content",
          label: copy.agendaContentLabel,
          title: copy.agendaContentTitle,
          description: copy.agendaContentDescription,
          href: "/cms",
          ctaLabel: copy.openCmsCta,
          bullets: [
            copy.agendaContentBulletOne,
            formatResource(copy.agendaContentBulletTwo, {
              status: pagesResult.status,
            }),
          ],
          meta: formatResource(copy.agendaContentMeta, {
            count: pagesResult.data?.items.length ?? 0,
          }),
        },
        {
          id: "agenda-commerce",
          label: copy.agendaCommerceLabel,
          title: copy.agendaCommerceTitle,
          description: copy.agendaCommerceDescription,
          href: "/catalog",
          ctaLabel: copy.browseCatalogCta,
          bullets: [
            copy.agendaCommerceBulletOne,
            formatResource(copy.agendaCommerceBulletTwo, {
              status: productsResult.status,
            }),
          ],
          meta: formatResource(copy.agendaCommerceMeta, {
            count: productsResult.data?.items.length ?? 0,
          }),
        },
        {
          id: "agenda-member",
          label: copy.agendaMemberLabel,
          title: copy.agendaMemberTitle,
          description: copy.agendaMemberDescription,
          href: "/account",
          ctaLabel: copy.openAccountCta,
          bullets: [
            copy.agendaMemberBulletOne,
            copy.agendaMemberBulletTwo,
          ],
          meta: formatResource(copy.agendaMemberMeta, {
            count: supportedCultures.length,
          }),
        },
      ],
      emptyMessage: copy.agendaEmptyMessage,
    },
    {
      id: "home-shortcuts",
      kind: "card-grid",
      eyebrow: copy.shortcutsEyebrow,
      title: copy.shortcutsTitle,
      description: copy.shortcutsDescription,
      cards: [
        {
          id: "shortcut-cms",
          eyebrow: copy.shortcutCmsEyebrow,
          title: copy.shortcutCmsTitle,
          description: copy.shortcutCmsDescription,
          href: "/cms",
          ctaLabel: copy.openCmsCta,
        },
        {
          id: "shortcut-catalog",
          eyebrow: copy.shortcutCatalogEyebrow,
          title: copy.shortcutCatalogTitle,
          description: copy.shortcutCatalogDescription,
          href: "/catalog",
          ctaLabel: copy.browseCatalogCta,
        },
        {
          id: "shortcut-account",
          eyebrow: copy.shortcutAccountEyebrow,
          title: copy.shortcutAccountTitle,
          description: copy.shortcutAccountDescription,
          href: "/account",
          ctaLabel: copy.openAccountCta,
        },
      ],
      emptyMessage: copy.shortcutsEmptyMessage,
    },
    {
      id: "home-journeys",
      kind: "link-list",
      eyebrow: copy.journeysEyebrow,
      title: copy.journeysTitle,
      description: copy.journeysDescription,
      items: [
        {
          id: "journey-cms",
          title: copy.journeyCmsTitle,
          description: copy.journeyCmsDescription,
          href: "/cms",
          ctaLabel: copy.readPageCta,
          meta: formatResource(copy.journeyCmsMeta, {
            status: pagesResult.status,
          }),
        },
        {
          id: "journey-catalog",
          title: copy.journeyCatalogTitle,
          description: copy.journeyCatalogDescription,
          href: "/catalog",
          ctaLabel: copy.browseCatalogCta,
          meta: formatResource(copy.journeyCatalogMeta, {
            status: productsResult.status,
          }),
        },
        {
          id: "journey-account",
          title: copy.journeyAccountTitle,
          description: copy.journeyAccountDescription,
          href: "/account",
          ctaLabel: copy.openAccountCta,
          meta: copy.journeyAccountMeta,
        },
      ],
      emptyMessage: copy.journeysEmptyMessage,
    },
    {
      id: "home-cms-spotlight",
      kind: "card-grid",
      eyebrow: copy.cmsSpotlightEyebrow,
      title: copy.cmsSpotlightTitle,
      description: copy.cmsSpotlightDescription,
      cards:
        pagesResult.data?.items.map((page) => ({
          id: page.id,
          eyebrow: copy.cmsPageEyebrow,
          title: page.title,
          description: page.metaDescription ?? copy.cmsPageDescriptionFallback,
          href: `/cms/${page.slug}`,
          ctaLabel: copy.readPageCta,
          meta: page.slug,
        })) ?? [],
      emptyMessage: pagesResult.message ?? copy.cmsSpotlightEmptyMessage,
    },
    {
      id: "home-product-spotlight",
      kind: "card-grid",
      eyebrow: copy.productSpotlightEyebrow,
      title: copy.productSpotlightTitle,
      description: copy.productSpotlightDescription,
      cards:
        productsResult.data?.items.map((product) => ({
          id: product.id,
          eyebrow: copy.productEyebrow,
          title: product.name,
          description: product.shortDescription ?? copy.productDescriptionFallback,
          href: `/catalog/${product.slug}`,
          ctaLabel: copy.viewProductCta,
          meta: formatMoney(product.priceMinor, product.currency, culture),
        })) ?? [],
      emptyMessage:
        productsResult.message ?? copy.productSpotlightEmptyMessage,
    },
  ];
}
