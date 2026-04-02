import {
  PublicContinuationRail,
  type PublicContinuationItem,
} from "@/components/shell/public-continuation-rail";
import { getMemberResource } from "@/localization";

type PublicAuthContinuationProps = {
  culture: string;
};

export function PublicAuthContinuation({
  culture,
}: PublicAuthContinuationProps) {
  const copy = getMemberResource(culture);

  const items: PublicContinuationItem[] = [
    {
      id: "auth-home",
      label: copy.memberCrossSurfaceTitle,
      title: copy.accountHubHomeTitle,
      description: copy.accountHubHomeDescription,
      href: "/",
      ctaLabel: copy.memberCrossSurfaceHomeCta,
    },
    {
      id: "auth-catalog",
      label: copy.accountHubCatalogLabel,
      title: copy.accountHubCatalogTitle,
      description: copy.accountHubCatalogDescription,
      href: "/catalog",
      ctaLabel: copy.memberCrossSurfaceCatalogCta,
    },
    {
      id: "auth-cms",
      label: copy.accountHubCmsLabel,
      title: copy.accountHubCmsTitle,
      description: copy.accountHubCmsDescription,
      href: "/cms",
      ctaLabel: copy.accountHubCmsCta,
    },
  ];

  return (
    <PublicContinuationRail
      culture={culture}
      eyebrow={copy.memberCrossSurfaceTitle}
      title={copy.accountHubRouteMapTitle}
      description={copy.accountHubRouteMapDescription}
      items={items}
    />
  );
}
