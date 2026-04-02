import {
  PublicContinuationRail,
  type PublicContinuationItem,
} from "@/components/shell/public-continuation-rail";
import { getMemberResource } from "@/localization";

type MemberCrossSurfaceRailProps = {
  culture: string;
  includeAccount?: boolean;
  includeOrders?: boolean;
  includeInvoices?: boolean;
  includeLoyalty?: boolean;
};

export function MemberCrossSurfaceRail({
  culture,
  includeAccount = true,
  includeOrders = false,
  includeInvoices = false,
  includeLoyalty = true,
}: MemberCrossSurfaceRailProps) {
  const copy = getMemberResource(culture);
  const items: PublicContinuationItem[] = [
    {
      id: "member-home",
      label: copy.memberCrossSurfaceHomeLabel,
      title: copy.memberCrossSurfaceHomeTitle,
      description: copy.memberCrossSurfaceHomeDescription,
      href: "/",
      ctaLabel: copy.memberCrossSurfaceHomeCta,
    },
    {
      id: "member-catalog",
      label: copy.memberCrossSurfaceCatalogLabel,
      title: copy.memberCrossSurfaceCatalogTitle,
      description: copy.memberCrossSurfaceCatalogDescription,
      href: "/catalog",
      ctaLabel: copy.memberCrossSurfaceCatalogCta,
    },
  ];

  if (includeAccount) {
    items.push({
      id: "member-account",
      label: copy.memberCrossSurfaceAccountLabel,
      title: copy.memberCrossSurfaceAccountTitle,
      description: copy.memberCrossSurfaceAccountDescription,
      href: "/account",
      ctaLabel: copy.memberCrossSurfaceAccountCta,
    });
  }

  if (includeOrders) {
    items.push({
      id: "member-orders",
      label: copy.memberCrossSurfaceOrdersLabel,
      title: copy.memberCrossSurfaceOrdersTitle,
      description: copy.memberCrossSurfaceOrdersDescription,
      href: "/orders",
      ctaLabel: copy.memberCrossSurfaceOrdersCta,
    });
  }

  if (includeInvoices) {
    items.push({
      id: "member-invoices",
      label: copy.memberCrossSurfaceInvoicesLabel,
      title: copy.memberCrossSurfaceInvoicesTitle,
      description: copy.memberCrossSurfaceInvoicesDescription,
      href: "/invoices",
      ctaLabel: copy.memberCrossSurfaceInvoicesCta,
    });
  }

  if (includeLoyalty) {
    items.push({
      id: "member-loyalty",
      label: copy.memberCrossSurfaceLoyaltyLabel,
      title: copy.memberCrossSurfaceLoyaltyTitle,
      description: copy.memberCrossSurfaceLoyaltyDescription,
      href: "/loyalty",
      ctaLabel: copy.memberCrossSurfaceLoyaltyCta,
    });
  }

  return (
    <PublicContinuationRail
      culture={culture}
      eyebrow={copy.memberCrossSurfaceTitle}
      title={copy.memberCrossSurfaceRailTitle}
      description={copy.memberCrossSurfaceMessage}
      items={items}
    />
  );
}
