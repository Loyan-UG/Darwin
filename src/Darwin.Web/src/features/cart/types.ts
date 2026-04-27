export type PublicCartItemRow = {
  variantId: string;
  quantity: number;
  unitPriceNetMinor: number;
  addOnPriceDeltaMinor: number;
  vatRate: number;
  lineNetMinor: number;
  lineVatMinor: number;
  lineGrossMinor: number;
  selectedAddOnValueIdsJson: string;
  selectedAddOns?: PublicCartSelectedAddOn[];
};

export type PublicCartSelectedAddOn = {
  valueId: string;
  optionId: string;
  optionLabel: string;
  valueLabel: string;
  priceDeltaMinor: number;
};

export type PublicCartSummary = {
  cartId: string;
  currency: string;
  items: PublicCartItemRow[];
  subtotalNetMinor: number;
  vatTotalMinor: number;
  grandTotalGrossMinor: number;
  couponCode?: string | null;
};

export type CartDisplaySnapshot = {
  variantId: string;
  name: string;
  href: string;
  imageUrl?: string | null;
  imageAlt?: string | null;
  sku?: string | null;
};
