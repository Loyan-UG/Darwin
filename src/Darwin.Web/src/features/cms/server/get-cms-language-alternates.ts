import "server-only";
import { getCmsLanguageAlternatesMap } from "@/features/cms/server/get-cms-language-alternates-map";

export async function getCmsLanguageAlternates(pageId: string) {
  const alternates = await getCmsLanguageAlternatesMap();
  return alternates.get(pageId) ?? {};
}
