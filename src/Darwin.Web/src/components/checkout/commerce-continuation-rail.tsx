import {
  PublicContinuationRail,
  type PublicContinuationItem,
} from "@/components/shell/public-continuation-rail";
import { getCommerceResource } from "@/localization";

type CommerceContinuationRailProps = {
  culture: string;
  includeHome?: boolean;
  includeCatalog?: boolean;
  includeCms?: boolean;
  includeCart?: boolean;
  includeAccount?: boolean;
};

export function CommerceContinuationRail({
  culture,
  includeHome = true,
  includeCatalog = true,
  includeCms = false,
  includeCart = false,
  includeAccount = true,
}: CommerceContinuationRailProps) {
  const copy = getCommerceResource(culture);
  const items: PublicContinuationItem[] = [];

  if (includeHome) {
    items.push({
      id: "commerce-home",
      label: copy.commerceContinuationHomeLabel,
      title: copy.commerceContinuationHomeTitle,
      description: copy.commerceContinuationHomeDescription,
      href: "/",
      ctaLabel: copy.commerceContinuationHomeCta,
    });
  }

  if (includeCatalog) {
    items.push({
      id: "commerce-catalog",
      label: copy.commerceContinuationCatalogLabel,
      title: copy.commerceContinuationCatalogTitle,
      description: copy.commerceContinuationCatalogDescription,
      href: "/catalog",
      ctaLabel: copy.commerceContinuationCatalogCta,
    });
  }

  if (includeCms) {
    items.push({
      id: "commerce-cms",
      label: copy.commerceContinuationCmsLabel,
      title: copy.commerceContinuationCmsTitle,
      description: copy.commerceContinuationCmsDescription,
      href: "/cms",
      ctaLabel: copy.commerceContinuationCmsCta,
    });
  }

  if (includeCart) {
    items.push({
      id: "commerce-cart",
      label: copy.commerceContinuationCartLabel,
      title: copy.commerceContinuationCartTitle,
      description: copy.commerceContinuationCartDescription,
      href: "/cart",
      ctaLabel: copy.commerceContinuationCartCta,
    });
  }

  if (includeAccount) {
    items.push({
      id: "commerce-account",
      label: copy.commerceContinuationAccountLabel,
      title: copy.commerceContinuationAccountTitle,
      description: copy.commerceContinuationAccountDescription,
      href: "/account",
      ctaLabel: copy.commerceContinuationAccountCta,
    });
  }

  return (
    <PublicContinuationRail
      culture={culture}
      eyebrow={copy.commerceContinuationEyebrow}
      title={copy.commerceContinuationTitle}
      description={copy.commerceContinuationMessage}
      items={items}
    />
  );
}
