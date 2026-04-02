import "server-only";

type CmsHeading = {
  id: string;
  text: string;
  level: 2 | 3;
};

type CmsContentSummary = {
  html: string;
  headings: CmsHeading[];
  wordCount: number;
  paragraphCount: number;
  readingMinutes: number;
};

const ENTITY_MAP: Record<string, string> = {
  "&amp;": "&",
  "&quot;": "\"",
  "&#39;": "'",
  "&lt;": "<",
  "&gt;": ">",
  "&nbsp;": " ",
};

function decodeEntities(value: string) {
  return value.replace(
    /&amp;|&quot;|&#39;|&lt;|&gt;|&nbsp;/g,
    (match) => ENTITY_MAP[match] ?? match,
  );
}

function stripTags(value: string) {
  return decodeEntities(value.replace(/<[^>]+>/g, " "))
    .replace(/\s+/g, " ")
    .trim();
}

function slugifyHeading(value: string) {
  const base = stripTags(value)
    .toLowerCase()
    .replace(/[^a-z0-9]+/g, "-")
    .replace(/^-+|-+$/g, "");

  return base || "section";
}

export function summarizeCmsContent(contentHtml: string): CmsContentSummary {
  const headings: CmsHeading[] = [];
  const slugCounts = new Map<string, number>();

  const html = contentHtml.replace(
    /<(h[23])([^>]*)>([\s\S]*?)<\/\1>/gi,
    (fullMatch, tagName: string, rawAttributes: string, innerHtml: string) => {
      const level = Number.parseInt(tagName.slice(1), 10) as 2 | 3;
      const existingIdMatch = rawAttributes.match(/\sid=(['"])(.*?)\1/i);
      const text = stripTags(innerHtml);

      if (!text) {
        return fullMatch;
      }

      const baseSlug = existingIdMatch?.[2] || slugifyHeading(innerHtml);
      const slugCount = slugCounts.get(baseSlug) ?? 0;
      slugCounts.set(baseSlug, slugCount + 1);
      const id = slugCount > 0 ? `${baseSlug}-${slugCount + 1}` : baseSlug;

      headings.push({
        id,
        text,
        level,
      });

      if (existingIdMatch) {
        return fullMatch;
      }

      return `<${tagName}${rawAttributes} id="${id}">${innerHtml}</${tagName}>`;
    },
  );

  const plainText = stripTags(html);
  const wordCount = plainText ? plainText.split(/\s+/).length : 0;
  const paragraphCount = (html.match(/<p\b/gi) ?? []).length;

  return {
    html,
    headings,
    wordCount,
    paragraphCount,
    readingMinutes: Math.max(1, Math.ceil(wordCount / 220)),
  };
}
