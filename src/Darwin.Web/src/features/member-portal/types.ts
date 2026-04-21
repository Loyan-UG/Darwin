export type MemberCustomerProfile = {
  id: string;
  email?: string | null;
  firstName?: string | null;
  lastName?: string | null;
  phoneE164?: string | null;
  phoneNumberConfirmed: boolean;
  locale?: string | null;
  timezone?: string | null;
  currency?: string | null;
  rowVersion?: string | null;
};

export type MemberPreferences = {
  rowVersion: string;
  marketingConsent: boolean;
  allowEmailMarketing: boolean;
  allowSmsMarketing: boolean;
  allowWhatsAppMarketing: boolean;
  allowPromotionalPushNotifications: boolean;
  allowOptionalAnalyticsTracking: boolean;
  acceptsTermsAtUtc?: string | null;
};

export type LinkedCustomerContext = {
  id: string;
  userId: string;
  displayName: string;
  email: string;
  phone?: string | null;
  companyName?: string | null;
  notes?: string | null;
  createdAtUtc: string;
  lastInteractionAtUtc?: string | null;
  interactionCount: number;
  segments: Array<{
    segmentId: string;
    name: string;
    description?: string | null;
  }>;
};

export type MemberAddress = {
  id: string;
  rowVersion: string;
  fullName: string;
  company?: string | null;
  street1: string;
  street2?: string | null;
  postalCode: string;
  city: string;
  state?: string | null;
  countryCode: string;
  phoneE164?: string | null;
  isDefaultBilling: boolean;
  isDefaultShipping: boolean;
};

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

export type MemberOrderSummary = {
  id: string;
  orderNumber: string;
  currency: string;
  grandTotalGrossMinor: number;
  status: string;
  createdAtUtc: string;
};

export type MemberOrderDetail = MemberOrderSummary & {
  subtotalNetMinor: number;
  taxTotalMinor: number;
  shippingTotalMinor: number;
  shippingMethodName?: string | null;
  shippingCarrier?: string | null;
  shippingService?: string | null;
  discountTotalMinor: number;
  billingAddressJson: string;
  shippingAddressJson: string;
  lines: Array<{
    id: string;
    variantId: string;
    name: string;
    sku: string;
    quantity: number;
    unitPriceGrossMinor: number;
    lineGrossMinor: number;
  }>;
  payments: Array<{
    id: string;
    createdAtUtc: string;
    provider: string;
    providerReference?: string | null;
    amountMinor: number;
    currency: string;
    status: string;
    paidAtUtc?: string | null;
  }>;
  shipments: Array<{
    id: string;
    carrier: string;
    service: string;
    trackingNumber?: string | null;
    trackingUrl?: string | null;
    status: string;
    shippedAtUtc?: string | null;
    deliveredAtUtc?: string | null;
  }>;
  invoices: Array<{
    id: string;
    currency: string;
    totalGrossMinor: number;
    status: string;
    dueDateUtc: string;
    paidAtUtc?: string | null;
  }>;
  actions: {
    canRetryPayment: boolean;
    paymentIntentPath?: string | null;
    confirmationPath: string;
    documentPath: string;
  };
};

export type MemberInvoiceSummary = {
  id: string;
  businessId?: string | null;
  businessName?: string | null;
  orderId?: string | null;
  orderNumber?: string | null;
  currency: string;
  totalGrossMinor: number;
  refundedAmountMinor: number;
  settledAmountMinor: number;
  balanceMinor: number;
  status: string;
  dueDateUtc: string;
  paidAtUtc?: string | null;
  createdAtUtc: string;
};

export type MemberInvoiceDetail = MemberInvoiceSummary & {
  totalNetMinor: number;
  totalTaxMinor: number;
  paymentSummary: string;
  lines: Array<{
    id: string;
    description: string;
    quantity: number;
    unitPriceNetMinor: number;
    taxRate: number;
    totalNetMinor: number;
    totalGrossMinor: number;
  }>;
  actions: {
    canRetryPayment: boolean;
    paymentIntentPath?: string | null;
    orderPath?: string | null;
    documentPath: string;
  };
};

export type LoyaltyAccountSummary = {
  businessId: string;
  businessName: string;
  pointsBalance: number;
  loyaltyAccountId: string;
  lifetimePoints: number;
  status: string;
  lastAccrualAtUtc?: string | null;
  nextRewardTitle?: string | null;
  nextRewardRequiredPoints?: number | null;
  pointsToNextReward?: number | null;
  nextRewardProgressPercent?: number | null;
};

export type MyLoyaltyOverview = {
  totalAccounts: number;
  activeAccounts: number;
  totalPointsBalance: number;
  totalLifetimePoints: number;
  lastAccrualAtUtc?: string | null;
  accounts: LoyaltyAccountSummary[];
};

export type MyLoyaltyBusinessSummary = {
  businessId: string;
  businessName: string;
  category: string;
  city?: string | null;
  location?: {
    latitude: number;
    longitude: number;
  } | null;
  primaryImageUrl?: string | null;
  pointsBalance: number;
  lifetimePoints: number;
  status: string;
  lastAccrualAtUtc?: string | null;
};

export type LoyaltyRewardSummary = {
  loyaltyRewardTierId: string;
  businessId: string;
  name: string;
  description?: string | null;
  requiredPoints: number;
  isActive: boolean;
  requiresConfirmation: boolean;
  isSelectable: boolean;
};

export type LoyaltyScanMode = "Accrual" | "Redemption";

export type PreparedMemberLoyaltyScanSession = {
  businessId: string;
  scanSessionToken: string;
  mode: LoyaltyScanMode;
  expiresAtUtc: string;
  currentPointsBalance: number;
  selectedRewards: LoyaltyRewardSummary[];
};

export type LoyaltyBusinessDashboard = {
  account: LoyaltyAccountSummary;
  availableRewardsCount: number;
  redeemableRewardsCount: number;
  nextReward?: LoyaltyRewardSummary | null;
  recentTransactions: PointsTransaction[];
  pointsToNextReward?: number | null;
  nextRewardRequiredPoints?: number | null;
  nextRewardProgressPercent?: number | null;
  expiryTrackingEnabled: boolean;
  pointsExpiringSoon: number;
  nextPointsExpiryAtUtc?: string | null;
};

export type PointsTransaction = {
  occurredAtUtc: string;
  type: string;
  delta: number;
  reference?: string | null;
  notes?: string | null;
};

export type LoyaltyTimelineEntry = {
  id: string;
  kind: string | number;
  loyaltyAccountId: string;
  businessId: string;
  occurredAtUtc: string;
  pointsDelta?: number | null;
  pointsSpent?: number | null;
  rewardTierId?: string | null;
  reference?: string | null;
  note?: string | null;
};

export type LoyaltyTimelinePage = {
  items: LoyaltyTimelineEntry[];
  nextBeforeAtUtc?: string | null;
  nextBeforeId?: string | null;
};

export type PromotionEligibilityRule = {
  audienceKind: string;
  minPoints?: number | null;
  maxPoints?: number | null;
  tierKey?: string | null;
  note?: string | null;
};

export type PromotionFeedItem = {
  businessId: string;
  businessName: string;
  title: string;
  description: string;
  ctaKind: string;
  priority: number;
  campaignId?: string | null;
  campaignState: string;
  startsAtUtc?: string | null;
  endsAtUtc?: string | null;
  eligibilityRules: PromotionEligibilityRule[];
};

export type PromotionFeedPolicy = {
  enableDeduplication: boolean;
  maxCards: number;
  frequencyWindowMinutes?: number | null;
  suppressionWindowMinutes?: number | null;
};

export type PromotionFeedDiagnostics = {
  initialCandidates: number;
  suppressedByFrequency: number;
  deduplicated: number;
  trimmedByCap: number;
  finalCount: number;
};

export type MyPromotionsResponse = {
  items: PromotionFeedItem[];
  appliedPolicy: PromotionFeedPolicy;
  diagnostics: PromotionFeedDiagnostics;
};
