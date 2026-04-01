import "server-only";
import { getPublicProducts } from "@/features/catalog/api/public-catalog";
import { getPublishedPages } from "@/features/cms/api/public-cms";
import type { WebPagePart } from "@/web-parts/types";

export async function getHomePageParts(): Promise<WebPagePart[]> {
  const [pagesResult, productsResult] = await Promise.all([
    getPublishedPages({
      page: 1,
      pageSize: 3,
    }),
    getPublicProducts({
      page: 1,
      pageSize: 3,
    }),
  ]);

  return [
    {
      id: "home-hero",
      kind: "hero",
      eyebrow: "Darwin.Web",
      title: "Storefront composition is now real, while the home page stays modular.",
      description:
        "The shell, CMS, catalog, cart, and checkout slices are now live against Darwin.WebApi. Home stays intentionally light, but it is no longer a single blank-state block.",
      actions: [
        { label: "Browse catalog", href: "/catalog" },
        { label: "Open checkout", href: "/checkout", variant: "secondary" },
      ],
      highlights: [
        "Theme tokens and shell chrome stay outside feature slices.",
        "Home sections are composed as explicit web parts instead of a one-off layout.",
        `CMS pages status: ${pagesResult.status}. Catalog status: ${productsResult.status}.`,
      ],
    },
    {
      id: "home-shortcuts",
      kind: "card-grid",
      eyebrow: "Storefront routes",
      title: "The current public surface is already navigable end to end.",
      description:
        "These shortcuts represent the currently implemented foundation before richer merchandising or campaign composition is added.",
      cards: [
        {
          id: "shortcut-cms",
          eyebrow: "Public CMS",
          title: "Published content pages",
          description:
            "Open the public CMS index and validate the same published page truth that WebAdmin now manages.",
          href: "/cms",
          ctaLabel: "Open CMS",
        },
        {
          id: "shortcut-catalog",
          eyebrow: "Catalog",
          title: "Catalog browsing",
          description:
            "Browse published categories and products directly from Darwin.WebApi without relying on starter placeholders.",
          href: "/catalog",
          ctaLabel: "Browse catalog",
        },
        {
          id: "shortcut-account",
          eyebrow: "Self-service",
          title: "Account actions",
          description:
            "Registration, activation, and password-reset self-service can grow here before full authenticated member sessions arrive.",
          href: "/account",
          ctaLabel: "Open account",
        },
      ],
      emptyMessage: "Storefront shortcuts are app-defined and should always be available.",
    },
    {
      id: "home-cms-spotlight",
      kind: "card-grid",
      eyebrow: "CMS spotlight",
      title: "Published pages are now available for storefront composition.",
      description:
        "This section is fed from the public CMS page list contract, not from a hard-coded marketing stub.",
      cards:
        pagesResult.data?.items.map((page) => ({
          id: page.id,
          eyebrow: "CMS page",
          title: page.title,
          description:
            page.metaDescription ??
            "Published storefront content available through the public CMS contract.",
          href: `/cms/${page.slug}`,
          ctaLabel: "Read page",
          meta: page.slug,
        })) ?? [],
      emptyMessage:
        pagesResult.message ??
        "No published CMS pages are available for the current home spotlight.",
    },
    {
      id: "home-product-spotlight",
      kind: "card-grid",
      eyebrow: "Storefront products",
      title: "Representative public products can now be surfaced on home.",
      description:
        "This section deliberately uses the current public catalog contract so later merchandising can extend from real data instead of a fake hero grid.",
      cards:
        productsResult.data?.items.map((product) => ({
          id: product.id,
          eyebrow: "Published product",
          title: product.name,
          description:
            product.shortDescription ??
            "Published product card delivered from the public catalog surface.",
          href: `/catalog/${product.slug}`,
          ctaLabel: "View product",
          meta: product.primaryImageUrl ? "Primary media available" : "Placeholder media path",
        })) ?? [],
      emptyMessage:
        productsResult.message ??
        "No published products are currently available for the home spotlight.",
    },
  ];
}
