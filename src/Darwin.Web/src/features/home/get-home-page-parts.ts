import "server-only";
import type { MemberSession } from "@/features/member-session/types";
import { getHomeDiscoveryContext } from "@/features/home/server/get-home-discovery-context";
import { getProductSavingsPercent } from "@/features/catalog/merchandising";
import { buildCatalogProductPath, buildCmsPagePath } from "@/lib/entity-paths";
import { formatMoney } from "@/lib/formatting";
import { buildAppQueryPath } from "@/lib/locale-routing";
import type { WebPagePart } from "@/web-parts/types";

type HomeCopy = {
  heroEyebrow: string;
  heroTitle: string;
  heroDescription: string;
  shopNow: string;
  loyaltyCta: string;
  accountCta: string;
  categoriesEyebrow: string;
  categoriesTitle: string;
  categoriesDescription: string;
  categoriesEmpty: string;
  categoryCta: string;
  offersEyebrow: string;
  offersTitle: string;
  offersDescription: string;
  offersEmpty: string;
  productCta: string;
  loyaltyEyebrow: string;
  loyaltyTitle: string;
  loyaltyDescription: string;
  loyaltyPrimary: string;
  loyaltySecondary: string;
  helpEyebrow: string;
  helpTitle: string;
  helpDescription: string;
  helpEmpty: string;
  helpCta: string;
  trustEyebrow: string;
  trustTitle: string;
  trustDescription: string;
  shippingTitle: string;
  shippingDescription: string;
  paymentTitle: string;
  paymentDescription: string;
  returnsTitle: string;
  returnsDescription: string;
};

function getCleanHomeCopy(culture: string): HomeCopy {
  if (culture.toLowerCase().startsWith("de")) {
    return {
      heroEyebrow: "Darwin Markt",
      heroTitle: "Frische Auswahl, einfache Bestellung und Vorteile fuer Mitglieder.",
      heroDescription:
        "Entdecken Sie Produkte, aktuelle Angebote und hilfreiche Services in einem klaren Storefront-Erlebnis.",
      shopNow: "Jetzt einkaufen",
      loyaltyCta: "Treueprogramm ansehen",
      accountCta: "Mein Konto",
      categoriesEyebrow: "Kategorien",
      categoriesTitle: "Schnell in die wichtigsten Bereiche.",
      categoriesDescription: "Starten Sie mit kuratierten Kategorien aus dem aktuellen Sortiment.",
      categoriesEmpty: "Kategorien sind voruebergehend nicht verfuegbar.",
      categoryCta: "Kategorie ansehen",
      offersEyebrow: "Aktuelle Auswahl",
      offersTitle: "Beliebte Produkte und Angebote.",
      offersDescription: "Eine kurze Auswahl aus dem aktuellen Katalog.",
      offersEmpty: "Produkte sind voruebergehend nicht verfuegbar.",
      productCta: "Produkt ansehen",
      loyaltyEyebrow: "Treue",
      loyaltyTitle: "Sammeln Sie Punkte bei teilnehmenden Unternehmen.",
      loyaltyDescription:
        "Melden Sie sich an, entdecken Sie Partner und verfolgen Sie Fortschritt und Praemien in Ihrem Konto.",
      loyaltyPrimary: "Treueprogramm oeffnen",
      loyaltySecondary: "Anmelden oder registrieren",
      helpEyebrow: "Hilfe und Informationen",
      helpTitle: "Antworten fuer Einkauf, Lieferung und Rueckgabe.",
      helpDescription: "Kurze Wege zu den wichtigsten Service- und Informationsseiten.",
      helpEmpty: "Hilfeseiten sind voruebergehend nicht verfuegbar.",
      helpCta: "Lesen",
      trustEyebrow: "Service",
      trustTitle: "Einfach einkaufen, sicher bezahlen, klar unterstuetzt.",
      trustDescription: "Die wichtigsten Zusagen fuer den Einkauf im Darwin Storefront.",
      shippingTitle: "Versand",
      shippingDescription: "Lieferoptionen werden im Checkout passend zur Adresse angezeigt.",
      paymentTitle: "Zahlung",
      paymentDescription: "Sichere Zahlungsuebergabe nach der Bestellpruefung.",
      returnsTitle: "Rueckgabe",
      returnsDescription: "Service- und Widerrufsinformationen bleiben schnell erreichbar.",
    };
  }

  return {
    heroEyebrow: "Darwin Market",
    heroTitle: "Fresh picks, simple checkout, and member rewards.",
    heroDescription:
      "Browse products, current offers, and helpful customer information in a clean storefront experience.",
    shopNow: "Shop now",
    loyaltyCta: "View loyalty",
    accountCta: "My account",
    categoriesEyebrow: "Categories",
    categoriesTitle: "Start with the most useful aisles.",
    categoriesDescription: "Browse curated categories from the current assortment.",
    categoriesEmpty: "Categories are temporarily unavailable.",
    categoryCta: "View category",
    offersEyebrow: "Current picks",
    offersTitle: "Featured products and offers.",
    offersDescription: "A short selection from the live catalog.",
    offersEmpty: "Products are temporarily unavailable.",
    productCta: "View product",
    loyaltyEyebrow: "Loyalty",
    loyaltyTitle: "Earn points with participating businesses.",
    loyaltyDescription:
      "Sign in, discover partners, and track reward progress from your member account.",
    loyaltyPrimary: "Open loyalty",
    loyaltySecondary: "Sign in or register",
    helpEyebrow: "Help and information",
    helpTitle: "Answers for shopping, shipping, and returns.",
    helpDescription: "Quick access to the most useful service and information pages.",
    helpEmpty: "Help pages are temporarily unavailable.",
    helpCta: "Read",
    trustEyebrow: "Service",
    trustTitle: "Easy shopping, secure payment, clear support.",
    trustDescription: "The core customer promises for shopping with Darwin.",
    shippingTitle: "Shipping",
    shippingDescription: "Delivery options are shown during checkout for the selected address.",
    paymentTitle: "Payment",
    paymentDescription: "Secure payment handoff after order review.",
    returnsTitle: "Returns",
    returnsDescription: "Return and cancellation information stays easy to find.",
  };
}

export async function getHomePageParts(
  culture: string,
  _session?: MemberSession | null,
  preloadedHomeDiscoveryContext?: Awaited<
    ReturnType<typeof getHomeDiscoveryContext>
  >,
): Promise<WebPagePart[]> {
  const copy = getCleanHomeCopy(culture);
  const homeDiscoveryContext =
    preloadedHomeDiscoveryContext ?? await getHomeDiscoveryContext(culture);
  const { pagesResult, productsResult, categoriesResult } = homeDiscoveryContext;
  const categories = (categoriesResult.data?.items ?? []).slice(0, 6);
  const products = (productsResult.data?.items ?? [])
    .slice()
    .sort((left, right) => {
      const leftSavings = getProductSavingsPercent(left) ?? 0;
      const rightSavings = getProductSavingsPercent(right) ?? 0;
      return rightSavings - leftSavings;
    })
    .slice(0, 8);
  const helpPages = (pagesResult.data?.items ?? [])
    .filter((page) =>
      /(faq|help|shipping|returns|contact|payment|warranty|support)/i.test(
        `${page.slug} ${page.title}`,
      ),
    )
    .slice(0, 3);
  const fallbackHelpPages =
    helpPages.length > 0 ? helpPages : (pagesResult.data?.items ?? []).slice(0, 3);

  return [
    {
      id: "home-hero",
      kind: "hero",
      eyebrow: copy.heroEyebrow,
      title: copy.heroTitle,
      description: copy.heroDescription,
      actions: [
        { label: copy.shopNow, href: "/catalog" },
        { label: copy.loyaltyCta, href: "/loyalty", variant: "secondary" },
        { label: copy.accountCta, href: "/account", variant: "secondary" },
      ],
      highlights: [
        copy.shippingDescription,
        copy.paymentDescription,
        copy.returnsDescription,
      ],
      panelTitle: copy.trustTitle,
    },
    {
      id: "home-category-spotlight",
      kind: "card-grid",
      eyebrow: copy.categoriesEyebrow,
      title: copy.categoriesTitle,
      description: copy.categoriesDescription,
      cards: categories.map((category) => ({
        id: category.id,
        title: category.name,
        description: category.description ?? copy.categoriesDescription,
        href: buildAppQueryPath("/catalog", { category: category.slug }),
        ctaLabel: copy.categoryCta,
      })),
      emptyMessage: copy.categoriesEmpty,
    },
    {
      id: "home-product-spotlight",
      kind: "card-grid",
      eyebrow: copy.offersEyebrow,
      title: copy.offersTitle,
      description: copy.offersDescription,
      cards: products.map((product) => ({
        id: product.id,
        eyebrow:
          (getProductSavingsPercent(product) ?? 0) > 0
            ? `${getProductSavingsPercent(product)}%`
            : undefined,
        title: product.name,
        description: product.shortDescription ?? copy.offersDescription,
        href: buildCatalogProductPath(product.slug),
        ctaLabel: copy.productCta,
        meta: formatMoney(product.priceMinor, product.currency, culture),
      })),
      emptyMessage: copy.offersEmpty,
    },
    {
      id: "home-loyalty-teaser",
      kind: "blank-state",
      eyebrow: copy.loyaltyEyebrow,
      title: copy.loyaltyTitle,
      description: copy.loyaltyDescription,
      actions: [
        { label: copy.loyaltyPrimary, href: "/loyalty" },
        { label: copy.loyaltySecondary, href: "/account", variant: "secondary" },
      ],
    },
    {
      id: "home-cms-spotlight",
      kind: "card-grid",
      eyebrow: copy.helpEyebrow,
      title: copy.helpTitle,
      description: copy.helpDescription,
      cards: fallbackHelpPages.map((page) => ({
        id: page.id,
        title: page.title,
        description: page.metaDescription ?? copy.helpDescription,
        href: buildCmsPagePath(page.slug),
        ctaLabel: copy.helpCta,
      })),
      emptyMessage: copy.helpEmpty,
    },
    {
      id: "home-trust-strip",
      kind: "card-grid",
      eyebrow: copy.trustEyebrow,
      title: copy.trustTitle,
      description: copy.trustDescription,
      cards: [
        {
          id: "trust-shipping",
          title: copy.shippingTitle,
          description: copy.shippingDescription,
          href: buildCmsPagePath("shipping"),
          ctaLabel: copy.helpCta,
        },
        {
          id: "trust-payment",
          title: copy.paymentTitle,
          description: copy.paymentDescription,
          href: buildCmsPagePath("payment"),
          ctaLabel: copy.helpCta,
        },
        {
          id: "trust-returns",
          title: copy.returnsTitle,
          description: copy.returnsDescription,
          href: buildCmsPagePath("returns"),
          ctaLabel: copy.helpCta,
        },
      ],
      emptyMessage: copy.helpEmpty,
    },
  ];
}
