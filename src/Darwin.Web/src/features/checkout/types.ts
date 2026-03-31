export type PublicCheckoutAddress = {
  fullName: string;
  company?: string | null;
  street1: string;
  street2?: string | null;
  postalCode: string;
  city: string;
  state?: string | null;
  countryCode: string;
  phoneE164?: string | null;
};

export type CheckoutDraft = {
  fullName: string;
  company: string;
  street1: string;
  street2: string;
  postalCode: string;
  city: string;
  state: string;
  countryCode: string;
  phoneE164: string;
  selectedShippingMethodId: string;
};

export type PublicShippingOption = {
  methodId: string;
  name: string;
  priceMinor: number;
  currency: string;
  carrier: string;
  service: string;
};

export type PublicCheckoutIntent = {
  cartId: string;
  currency: string;
  subtotalNetMinor: number;
  vatTotalMinor: number;
  grandTotalGrossMinor: number;
  shipmentMass: number;
  requiresShipping: boolean;
  shippingCountryCode?: string | null;
  selectedShippingMethodId?: string | null;
  selectedShippingTotalMinor: number;
  shippingOptions: PublicShippingOption[];
};

export type PlaceOrderFromCartResponse = {
  orderId: string;
  orderNumber: string;
  currency: string;
  grandTotalGrossMinor: number;
  status: string;
};

export type PublicStorefrontPaymentIntent = {
  orderId: string;
  paymentId: string;
  provider: string;
  providerReference: string;
  amountMinor: number;
  currency: string;
  status: string;
  checkoutUrl: string;
  returnUrl: string;
  cancelUrl: string;
  expiresAtUtc: string;
};

export type PublicStorefrontOrderConfirmationLine = {
  id: string;
  variantId: string;
  name: string;
  sku: string;
  quantity: number;
  unitPriceGrossMinor: number;
  lineGrossMinor: number;
};

export type PublicStorefrontOrderConfirmationPayment = {
  id: string;
  provider: string;
  providerReference?: string | null;
  amountMinor: number;
  currency: string;
  status: string;
  paidAtUtc?: string | null;
};

export type PublicStorefrontOrderConfirmation = {
  orderId: string;
  orderNumber: string;
  currency: string;
  subtotalNetMinor: number;
  taxTotalMinor: number;
  shippingTotalMinor: number;
  shippingMethodId?: string | null;
  shippingMethodName?: string | null;
  shippingCarrier?: string | null;
  shippingService?: string | null;
  discountTotalMinor: number;
  grandTotalGrossMinor: number;
  status: string;
  billingAddressJson: string;
  shippingAddressJson: string;
  createdAtUtc: string;
  lines: PublicStorefrontOrderConfirmationLine[];
  payments: PublicStorefrontOrderConfirmationPayment[];
};
