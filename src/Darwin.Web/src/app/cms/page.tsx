import { redirect } from "next/navigation";
import {
  readPositiveIntegerSearchParam,
  readSearchTextParam,
} from "@/features/checkout/helpers";
import { buildAppQueryPath, localizeHref } from "@/lib/locale-routing";
import { getRequestCulture } from "@/lib/request-culture";

type HelpRedirectRouteProps = {
  searchParams?: Promise<{
    page?: string;
    search?: string;
  }>;
};

export default async function HelpRedirectRoute({
  searchParams,
}: HelpRedirectRouteProps) {
  const resolvedSearchParams = searchParams ? await searchParams : undefined;
  const culture = await getRequestCulture();
  const safePage = readPositiveIntegerSearchParam(resolvedSearchParams?.page);
  const searchQuery = readSearchTextParam(resolvedSearchParams?.search);

  redirect(
    localizeHref(
      buildAppQueryPath("/help", {
        page: safePage > 1 ? safePage : undefined,
        search: searchQuery,
      }),
      culture,
    ),
  );
}
