import { buildLocalizedPath } from "@/lib/locale-routing";
import { getSiteRuntimeConfig } from "@/lib/site-runtime-config";

type LocalizedDetailItem = {
  id: string;
  slug: string;
};

type LocalizedDetailSet<TItem extends LocalizedDetailItem> = {
  culture: string;
  items: TItem[];
};

export function groupLocalizedDetailAlternates<
  TItem extends LocalizedDetailItem,
>(
  localizedSets: Array<LocalizedDetailSet<TItem>>,
  toPath: (slug: string) => string,
) {
  const { defaultCulture } = getSiteRuntimeConfig();
  const alternatesById = mapLocalizedDetailAlternatesById(localizedSets, toPath);

  return Array.from(alternatesById.values())
    .map((languageAlternates) => {
      const canonicalPath =
        languageAlternates[defaultCulture] ??
        Object.values(languageAlternates)[0];

      return canonicalPath
        ? {
            path: canonicalPath,
            languageAlternates,
          }
        : null;
    })
    .filter(
      (
        entry,
      ): entry is {
        path: string;
        languageAlternates: Record<string, string>;
      } => Boolean(entry),
    );
}

export function mapLocalizedDetailAlternatesById<
  TItem extends LocalizedDetailItem,
>(
  localizedSets: Array<LocalizedDetailSet<TItem>>,
  toPath: (slug: string) => string,
) {
  const alternatesById = new Map<string, Record<string, string>>();

  for (const localizedSet of localizedSets) {
    for (const item of localizedSet.items) {
      if (!item.id || !item.slug) {
        continue;
      }

      const alternates = alternatesById.get(item.id) ?? {};
      alternates[localizedSet.culture] = buildLocalizedPath(
        toPath(item.slug),
        localizedSet.culture,
      );
      alternatesById.set(item.id, alternates);
    }
  }

  return alternatesById;
}
