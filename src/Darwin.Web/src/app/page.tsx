import { PageComposer } from "@/web-parts/page-composer";
import { getHomePageView } from "@/features/home/server/get-home-page-view";
import { getHomeSeoMetadata } from "@/features/home/server/get-home-seo-metadata";
import { getRequestCulture } from "@/lib/request-culture";

export async function generateMetadata() {
  const culture = await getRequestCulture();
  const { metadata } = await getHomeSeoMetadata(culture);
  return metadata;
}

export default async function Home() {
  const culture = await getRequestCulture();
  const view = await getHomePageView(culture);

  return <PageComposer parts={view.parts} culture={view.culture} />;
}
