import "server-only";
import { buildAccountEntryView } from "@/features/account/account-entry-view";
import { getAccountPageContext } from "@/features/account/server/get-account-page-context";

export async function getAccountPageView(
  culture: string,
  returnPath?: string,
) {
  const pageContext = await getAccountPageContext(culture);

  return buildAccountEntryView({
    culture,
    returnPath,
    ...pageContext,
  });
}
