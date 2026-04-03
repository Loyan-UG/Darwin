import "server-only";
import { getHomeRouteContext } from "@/features/home/server/get-home-route-context";

export async function getHomePageView(culture: string) {
  const { parts } = await getHomeRouteContext(culture);

  return {
    culture,
    parts,
  };
}
