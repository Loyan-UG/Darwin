import type { LoyaltyAccountSummary } from "@/features/member-portal/types";

export type BusinessCategoryKind = {
  value: number;
  key: string;
  displayName: string;
};

export type GeoCoordinate = {
  latitude: number;
  longitude: number;
  altitudeMeters?: number | null;
};

export type BusinessSummary = {
  id: string;
  name: string;
  shortDescription?: string | null;
  logoUrl?: string | null;
  category: string;
  rating?: number | null;
  ratingCount?: number | null;
  location?: GeoCoordinate | null;
  city?: string | null;
  isOpenNow?: boolean | null;
  isActive: boolean;
  distanceMeters?: number | null;
};

export type BusinessLocation = {
  businessLocationId: string;
  name: string;
  addressLine1?: string | null;
  addressLine2?: string | null;
  city?: string | null;
  region?: string | null;
  countryCode?: string | null;
  postalCode?: string | null;
  coordinate?: GeoCoordinate | null;
  isPrimary: boolean;
  openingHoursJson?: string | null;
};

export type LoyaltyRewardTierPublic = {
  id: string;
  pointsRequired: number;
  rewardType: string;
  rewardValue?: number | null;
  description?: string | null;
  allowSelfRedemption: boolean;
};

export type LoyaltyProgramPublic = {
  id: string;
  businessId: string;
  name: string;
  isActive: boolean;
  rewardTiers: LoyaltyRewardTierPublic[];
};

export type BusinessDetail = {
  id: string;
  name: string;
  category: string;
  shortDescription?: string | null;
  description?: string | null;
  primaryImageUrl?: string | null;
  galleryImageUrls?: string[] | null;
  imageUrls?: string[] | null;
  city?: string | null;
  coordinate?: GeoCoordinate | null;
  openingHours?: unknown;
  phoneE164?: string | null;
  defaultCurrency: string;
  defaultCulture: string;
  websiteUrl?: string | null;
  contactEmail?: string | null;
  contactPhoneE164?: string | null;
  locations: BusinessLocation[];
  loyaltyProgram?: unknown;
  loyaltyProgramPublic?: LoyaltyProgramPublic | null;
};

export type BusinessDetailWithMyAccount = {
  business: BusinessDetail;
  hasAccount: boolean;
  myAccount?: LoyaltyAccountSummary | null;
};
