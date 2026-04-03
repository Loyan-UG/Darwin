import type {
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

export function filterCatalogVisibleProducts(
  products: PublicProductSummary[],
  visibleState: CatalogVisibleState,
  visibleQuery?: string,
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
