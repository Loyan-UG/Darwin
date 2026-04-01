import { PageComposer } from "@/web-parts/page-composer";
import { getHomePageParts } from "@/features/home/get-home-page-parts";

export default async function Home() {
  const homeParts = await getHomePageParts();
  return <PageComposer parts={homeParts} />;
}
