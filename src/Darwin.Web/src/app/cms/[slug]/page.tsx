import { redirect } from "next/navigation";
import { buildCmsPagePath } from "@/lib/entity-paths";
import { buildAppQueryPath } from "@/lib/locale-routing";

type LegacyCmsDetailRedirectProps = {
  params: Promise<{
    slug: string;
  }>;
  searchParams?: Promise<Record<string, string | string[] | undefined>>;
};

function firstSearchValue(value: string | string[] | undefined) {
  return Array.isArray(value) ? value[0] : value;
}

export default async function LegacyCmsDetailRedirect({
  params,
  searchParams,
}: LegacyCmsDetailRedirectProps) {
  const { slug } = await params;
  const resolvedSearchParams = searchParams ? await searchParams : undefined;

  redirect(
    buildAppQueryPath(buildCmsPagePath(slug), {
      culture: firstSearchValue(resolvedSearchParams?.culture),
      _darwinInferredCulture: firstSearchValue(
        resolvedSearchParams?._darwinInferredCulture,
      ),
    }),
  );
}
