import "server-only";

function stripDangerousBlocks(value: string) {
  return value.replace(
    /<(script|iframe|object|embed|link|meta|base|form)\b[\s\S]*?<\/\1>/gi,
    "",
  );
}

function stripDangerousStandaloneTags(value: string) {
  return value.replace(/<(script|iframe|object|embed|link|meta|base|form)\b[^>]*\/?>/gi, "");
}

function stripInlineEventHandlers(value: string) {
  return value
    .replace(/\son[a-z]+\s*=\s*(['"]).*?\1/gi, "")
    .replace(/\son[a-z]+\s*=\s*[^\s>]+/gi, "");
}

function stripJavascriptUrls(value: string) {
  return value
    .replace(
      /\s(href|src)\s*=\s*(['"])\s*javascript:[\s\S]*?\2/gi,
      "",
    )
    .replace(/\s(href|src)\s*=\s*javascript:[^\s>]+/gi, "");
}

export function sanitizeHtmlFragment(value: string) {
  if (!value) {
    return "";
  }

  return stripJavascriptUrls(
    stripInlineEventHandlers(
      stripDangerousStandaloneTags(
        stripDangerousBlocks(value),
      ),
    ),
  );
}
