import { PageComposer } from "@/web-parts/page-composer";
import { getHomePageParts } from "@/features/home/get-home-page-parts";
import { getMemberSession } from "@/features/member-session/cookies";
import { getSharedResource } from "@/localization";
import { getRequestCulture } from "@/lib/request-culture";
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
  const session = await getMemberSession();
  const homeParts = await getHomePageParts(culture, session);
  return <PageComposer parts={homeParts} culture={culture} />;
}
