import {
  PublicContinuationRail,
  type PublicContinuationItem,
} from "@/components/shell/public-continuation-rail";
import { getSharedResource } from "@/localization";

type CmsContinuationRailProps = {
  culture: string;
  includeHome?: boolean;
  includeCatalog?: boolean;
  includeAccount?: boolean;
  title?: string;
  description?: string;
};

export function CmsContinuationRail({
  culture,
  includeHome = true,
  includeCatalog = true,
  includeAccount = true,
  title,
  description,
}: CmsContinuationRailProps) {
  const copy = getSharedResource(culture);
  const items: PublicContinuationItem[] = [];

  if (includeHome) {
    items.push({
      id: "cms-home",
      label: copy.cmsCrossSurfaceHomeLabel,
      title: copy.cmsCrossSurfaceHomeTitle,
      description: copy.cmsCrossSurfaceHomeDescription,
      href: "/",
      ctaLabel: copy.cmsFollowUpHomeCta,
    });
  }

  if (includeCatalog) {
    items.push({
      id: "cms-catalog",
      label: copy.cmsCrossSurfaceCatalogLabel,
      title: copy.cmsCrossSurfaceCatalogTitle,
      description: copy.cmsCrossSurfaceCatalogDescription,
      href: "/catalog",
      ctaLabel: copy.cmsFollowUpCatalogCta,
    });
  }

  if (includeAccount) {
    items.push({
      id: "cms-account",
      label: copy.cmsCrossSurfaceAccountLabel,
      title: copy.cmsCrossSurfaceAccountTitle,
      description: copy.cmsCrossSurfaceAccountDescription,
      href: "/account",
      ctaLabel: copy.cmsFollowUpAccountCta,
    });
  }

  return (
    <PublicContinuationRail
      culture={culture}
      eyebrow={copy.cmsFollowUpTitle}
      title={title ?? copy.cmsCrossSurfaceTitle}
      description={description ?? copy.cmsFollowUpDescription}
      items={items}
    />
  );
}
