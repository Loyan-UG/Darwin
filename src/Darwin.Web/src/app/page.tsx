import { PageComposer } from "@/web-parts/page-composer";
import { getHomePageParts } from "@/features/home/get-home-page-parts";
import { getMemberSession } from "@/features/member-session/cookies";
import { getSharedResource } from "@/localization";
import { getRequestCulture } from "@/lib/request-culture";
import { observeAsyncOperation } from "@/lib/route-observability";
import { buildSeoMetadata } from "@/lib/seo";

export async function generateMetadata() {
  const culture = await getRequestCulture();
  const shared = getSharedResource(culture);

  return buildSeoMetadata({
    culture,
    title: shared.homeMetaTitle,
    description: shared.homeMetaDescription,
    path: "/",
    allowLanguageAlternates: true,
  });
}

export default async function Home() {
  const culture = await getRequestCulture();
  const homeParts = await observeAsyncOperation(
    {
      area: "home",
      operation: "load-route",
      thresholdMs: 350,
    },
    async () => {
      const memberSession = await getMemberSession();
      return getHomePageParts(culture, memberSession);
    },
  );

  return <PageComposer parts={homeParts} culture={culture} />;
}
