import { PageComposer } from "@/web-parts/page-composer";
import type { WebPagePart } from "@/web-parts/types";

const homeParts: WebPagePart[] = [
  {
    id: "home-intro",
    kind: "blank-state",
    eyebrow: "Darwin.Web",
    title: "Storefront shell is ready. Home composition stays intentionally light for now.",
    description:
      "This first slice establishes the theme boundary, CMS-backed navigation, route scaffolding, and page-part system. We can return later to design the full homepage with richer merchandising blocks.",
    actions: [
      { label: "Browse catalog", href: "/catalog" },
      { label: "Open account area", href: "/account", variant: "secondary" },
    ],
  },
];

export default function Home() {
  return <PageComposer parts={homeParts} />;
}
