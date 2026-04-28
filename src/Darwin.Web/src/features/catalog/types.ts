export type PagedRequest = {
  page: number;
  pageSize: number;
  search?: string | null;
};

export type PagedResponse<T> = {
  total: number;
  items: T[];
  request: PagedRequest;
};

export type PublicCategorySummary = {
  id: string;
  parentId?: string | null;
  name: string;
  slug: string;
  description?: string | null;
  metaTitle?: string | null;
  metaDescription?: string | null;
  sortOrder: number;
};

export type PublicProductSummary = {
  id: string;
  name: string;
  slug: string;
  shortDescription?: string | null;
  currency: string;
  priceMinor: number;
  compareAtPriceMinor?: number | null;
  primaryImageUrl?: string | null;
};

export type PublicProductVariant = {
  id: string;
  sku: string;
  currency: string;
  basePriceNetMinor: number;
  compareAtPriceNetMinor?: number | null;
  backorderAllowed: boolean;
  isDigital: boolean;
};

export type PublicProductMedia = {
  id: string;
  url: string;
  alt: string;
  title?: string | null;
  role?: string | null;
  sortOrder: number;
};

export type PublicProductAddOnValue = {
  id: string;
  label: string;
  priceDeltaMinor: number;
  hint?: string | null;
  sortOrder: number;
};

export type PublicProductAddOnOption = {
  id: string;
  label: string;
  sortOrder: number;
  values: PublicProductAddOnValue[];
};

export type PublicProductAddOnGroup = {
  id: string;
  name: string;
  currency: string;
  selectionMode: string;
  minSelections: number;
  maxSelections?: number | null;
  options: PublicProductAddOnOption[];
};

export type PublicProductDetail = PublicProductSummary & {
  fullDescriptionHtml?: string | null;
  metaTitle?: string | null;
  metaDescription?: string | null;
  primaryCategoryId?: string | null;
  variants: PublicProductVariant[];
  media: PublicProductMedia[];
  applicableAddOns?: PublicProductAddOnGroup[];
};

export type CatalogVisibleSort =
  | "featured"
  | "name-asc"
  | "price-asc"
  | "price-desc"
  | "savings-desc"
  | "offers-first"
  | "base-first";

export type CatalogVisibleState = "all" | "offers" | "base";
export type CatalogMediaState = "all" | "with-image" | "missing-image";
export type CatalogSavingsBand = "all" | "value" | "hero";
