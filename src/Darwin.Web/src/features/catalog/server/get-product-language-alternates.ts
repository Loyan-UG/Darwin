import "server-only";
import { getProductLanguageAlternatesMap } from "@/features/catalog/server/get-product-language-alternates-map";

export async function getProductLanguageAlternates(productId: string) {
  const alternates = await getProductLanguageAlternatesMap();
  return alternates.get(productId) ?? {};
}
