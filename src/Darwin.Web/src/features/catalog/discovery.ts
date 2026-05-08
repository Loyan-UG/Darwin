import type {
  CatalogMediaState,
  CatalogSavingsBand,
  CatalogVisibleSort,
  CatalogVisibleState,
  PublicProductSummary,
} from "@/features/catalog/types";
import { readAllowedSearchParam } from "@/features/checkout/helpers";

export function readCatalogVisibleState(value?: string): CatalogVisibleState {
  return (
    readAllowedSearchParam(value, ["all", "offers", "base"] as const) ?? "all"
  );
}

export function readCatalogVisibleSort(value?: string): CatalogVisibleSort {
  return (
    readAllowedSearchParam(value, [
      "featured",
      "name-asc",
      "price-asc",
      "price-desc",
      "savings-desc",
      "offers-first",
      "base-first",
    ] as const) ?? "featured"
  );
}

export function readCatalogMediaState(value?: string): CatalogMediaState {
  return (
    readAllowedSearchParam(
      value,
      ["all", "with-image", "missing-image"] as const,
    ) ?? "all"
  );
}

export function readCatalogSavingsBand(value?: string): CatalogSavingsBand {
  return (
    readAllowedSearchParam(value, ["all", "value", "hero"] as const) ?? "all"
  );
}

export type CatalogQualityTarget = {
  product: PublicProductSummary;
  missingImage: boolean;
  savingsAmount: number;
};

export function getCatalogSavingsAmount(product: PublicProductSummary) {
  if (
    typeof product.compareAtPriceMinor !== "number" ||
    product.compareAtPriceMinor <= product.priceMinor
  ) {
    return 0;
  }

  return product.compareAtPriceMinor - product.priceMinor;
}

export function isOfferProduct(product: PublicProductSummary) {
  return getCatalogSavingsAmount(product) > 0;
}

export function hasPrimaryImage(product: PublicProductSummary) {
  return Boolean(product.primaryImageUrl?.trim());
}

export function getCatalogSavingsPercent(product: PublicProductSummary) {
  if (
    typeof product.compareAtPriceMinor !== "number" ||
    product.compareAtPriceMinor <= product.priceMinor
  ) {
    return 0;
  }

  return Math.round(
    ((product.compareAtPriceMinor - product.priceMinor) /
      product.compareAtPriceMinor) *
      100,
  );
}

export function getCatalogQualityTarget(
  product: PublicProductSummary,
): CatalogQualityTarget {
  return {
    product,
    missingImage: !product.primaryImageUrl?.trim(),
    savingsAmount: getCatalogSavingsAmount(product),
  };
}

export function sortCatalogQualityTargets(targets: CatalogQualityTarget[]) {
  return [...targets].sort((left, right) => {
    if (left.missingImage !== right.missingImage) {
      return left.missingImage ? -1 : 1;
    }

    if (right.savingsAmount !== left.savingsAmount) {
      return right.savingsAmount - left.savingsAmount;
    }

    return left.product.name.localeCompare(right.product.name);
  });
}

export function getCatalogQualityTargets(products: PublicProductSummary[]) {
  return sortCatalogQualityTargets(products.map(getCatalogQualityTarget));
}

export function filterCatalogVisibleProducts(
  products: PublicProductSummary[],
  visibleState: CatalogVisibleState,
  visibleQuery?: string,
  mediaState: CatalogMediaState = "all",
  savingsBand: CatalogSavingsBand = "all",
) {
  const normalizedQuery = visibleQuery?.trim().toLowerCase();

  return products.filter((product) => {
    if (normalizedQuery) {
      const haystack = [product.name, product.shortDescription]
        .filter(Boolean)
        .join(" ")
        .toLowerCase();

      if (!haystack.includes(normalizedQuery)) {
        return false;
      }
    }

    if (visibleState === "offers") {
      return isOfferProduct(product);
    }

    if (visibleState === "base") {
      return !isOfferProduct(product);
    }

    if (mediaState === "with-image" && !hasPrimaryImage(product)) {
      return false;
    }

    if (mediaState === "missing-image" && hasPrimaryImage(product)) {
      return false;
    }

    const savingsPercent = getCatalogSavingsPercent(product);

    if (savingsBand === "value" && savingsPercent < 10) {
      return false;
    }

    if (savingsBand === "hero" && savingsPercent < 25) {
      return false;
    }

    return true;
  });
}

export function sortCatalogVisibleProducts(
  products: PublicProductSummary[],
  visibleSort: CatalogVisibleSort,
) {
  const rankedProducts = [...products];

  switch (visibleSort) {
    case "name-asc":
      rankedProducts.sort((left, right) => left.name.localeCompare(right.name));
      break;
    case "price-asc":
      rankedProducts.sort((left, right) => left.priceMinor - right.priceMinor);
      break;
    case "price-desc":
      rankedProducts.sort((left, right) => right.priceMinor - left.priceMinor);
      break;
    case "savings-desc":
      rankedProducts.sort(
        (left, right) =>
          getCatalogSavingsAmount(right) - getCatalogSavingsAmount(left),
      );
      break;
    case "offers-first":
      rankedProducts.sort((left, right) => {
        const offerDelta = Number(isOfferProduct(right)) - Number(isOfferProduct(left));
        if (offerDelta !== 0) {
          return offerDelta;
        }

        return left.name.localeCompare(right.name);
      });
      break;
    case "base-first":
      rankedProducts.sort((left, right) => {
        const baseDelta = Number(!isOfferProduct(right)) - Number(!isOfferProduct(left));
        if (baseDelta !== 0) {
          return baseDelta;
        }

        return left.name.localeCompare(right.name);
      });
      break;
    case "featured":
    default:
      break;
  }

  return rankedProducts;
}

export function buildCatalogVisibleWindow(
  products: PublicProductSummary[],
  input: {
    page: number;
    pageSize: number;
    visibleState: CatalogVisibleState;
    visibleSort: CatalogVisibleSort;
    mediaState?: CatalogMediaState;
    savingsBand?: CatalogSavingsBand;
  },
) {
  const filteredProducts = sortCatalogVisibleProducts(
    filterCatalogVisibleProducts(
      products,
      input.visibleState,
      undefined,
      input.mediaState,
      input.savingsBand,
    ),
    input.visibleSort,
  );
  const total = filteredProducts.length;
  const totalPages = Math.max(1, Math.ceil(total / input.pageSize));
  const currentPage = Math.min(Math.max(input.page, 1), totalPages);
  const start = (currentPage - 1) * input.pageSize;

  return {
    items: filteredProducts.slice(start, start + input.pageSize),
    total,
    totalPages,
    currentPage,
  };
}

export function summarizeCatalogFacets(products: PublicProductSummary[]) {
  const withImageCount = products.filter(hasPrimaryImage).length;
  const missingImageCount = products.length - withImageCount;
  const offerCount = products.filter(isOfferProduct).length;
  const baseCount = products.length - offerCount;
  const valueOfferCount = products.filter(
    (product) => getCatalogSavingsPercent(product) >= 10,
  ).length;
  const heroOfferCount = products.filter(
    (product) => getCatalogSavingsPercent(product) >= 25,
  ).length;

  return {
    totalCount: products.length,
    offerCount,
    baseCount,
    withImageCount,
    missingImageCount,
    valueOfferCount,
    heroOfferCount,
  };
}
