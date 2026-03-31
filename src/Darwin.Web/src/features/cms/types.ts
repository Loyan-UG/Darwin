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

export type PublicMenuItem = {
  id: string;
  parentId?: string | null;
  label: string;
  url: string;
  sortOrder: number;
};

export type PublicMenu = {
  id: string;
  name: string;
  items: PublicMenuItem[];
};

export type PublicPageDetail = {
  id: string;
  title: string;
  slug: string;
  metaTitle?: string | null;
  metaDescription?: string | null;
  contentHtml: string;
};

export type PublicPageSummary = {
  id: string;
  title: string;
  slug: string;
  metaTitle?: string | null;
  metaDescription?: string | null;
};
