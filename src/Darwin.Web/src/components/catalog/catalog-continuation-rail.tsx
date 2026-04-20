import {
  PublicContinuationRail,
  type PublicContinuationItem,
} from "@/components/shell/public-continuation-rail";
import { getCatalogResource } from "@/localization";

type CatalogContinuationRailProps = {
  culture: string;
  includeHome?: boolean;
  includeCatalog?: boolean;
  includeCms?: boolean;
  includeAccount?: boolean;
  catalogHref?: string;
  catalogCtaLabel?: string;
  title?: string;
  description?: string;
  items?: PublicContinuationItem[];
};

export function CatalogContinuationRail({
  culture,
  includeHome = true,
  includeCatalog = true,
  includeCms = true,
  includeAccount = true,
  catalogHref = "/catalog",
  catalogCtaLabel,
  title,
  description,
  items = [],
}: CatalogContinuationRailProps) {
  const copy = getCatalogResource(culture);
  const railItems: PublicContinuationItem[] = [...items];

  if (includeHome) {
    railItems.push({
      id: "catalog-home",
      label: copy.productCrossSurfaceHomeLabel,
      title: copy.productCrossSurfaceHomeTitle,
      description: copy.productCrossSurfaceHomeDescription,
      href: "/",
      ctaLabel: copy.productCrossSurfaceHomeCta,
    });
  }

  if (includeCatalog) {
    railItems.push({
      id: "catalog-catalog",
      label: copy.productCrossSurfaceCatalogLabel,
      title: copy.productCrossSurfaceCatalogTitle,
      description: copy.productCrossSurfaceCatalogDescription,
      href: catalogHref,
      ctaLabel: catalogCtaLabel ?? copy.backToCatalog,
    });
  }

  if (includeCms) {
    railItems.push({
      id: "catalog-cms",
      label: copy.productCrossSurfaceCmsLabel,
      title: copy.productCrossSurfaceCmsTitle,
      description: copy.productCrossSurfaceCmsDescription,
      href: "/cms",
      ctaLabel: copy.productUnavailableCmsCta,
    });
  }

  if (includeAccount) {
    railItems.push({
      id: "catalog-account",
      label: copy.productCrossSurfaceAccountLabel,
      title: copy.productCrossSurfaceAccountTitle,
      description: copy.productCrossSurfaceAccountDescription,
      href: "/account",
      ctaLabel: copy.productCrossSurfaceAccountCta,
    });
  }

  return (
    <PublicContinuationRail
      culture={culture}
      eyebrow={copy.productCrossSurfaceTitle}
      title={title ?? copy.productCrossSurfaceGridTitle}
      description={description ?? copy.productCrossSurfaceMessage}
      items={railItems}
    />
  );
}
