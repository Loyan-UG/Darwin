import test from "node:test";
import assert from "node:assert/strict";
import { sanitizeHtmlFragment } from "@/lib/html-fragment";

test("sanitizeHtmlFragment removes dangerous blocks and inline handlers", () => {
  const html = `
    <div onclick="alert('xss')">
      <script>alert("xss")</script>
      <a href="javascript:alert('xss')" onmouseover=test()>Open</a>
      <iframe src="https://example.com/embed"></iframe>
      <p>Safe text</p>
    </div>
  `;

  const result = sanitizeHtmlFragment(html);

  assert.match(result, /<p>Safe text<\/p>/);
  assert.doesNotMatch(result, /<script/i);
  assert.doesNotMatch(result, /<iframe/i);
  assert.doesNotMatch(result, /\sonclick=/i);
  assert.doesNotMatch(result, /\sonmouseover=/i);
  assert.doesNotMatch(result, /javascript:/i);
});

test("sanitizeHtmlFragment also strips unquoted javascript URLs and handlers", () => {
  const html =
    '<img src=javascript:alert(1) onerror=alert(2)><a href=javascript:alert(3)>Unsafe</a><span>Keep me</span>';

  const result = sanitizeHtmlFragment(html);

  assert.match(result, /<span>Keep me<\/span>/);
  assert.doesNotMatch(result, /\sonerror=/i);
  assert.doesNotMatch(result, /\shref\s*=/i);
  assert.doesNotMatch(result, /\ssrc\s*=/i);
  assert.doesNotMatch(result, /javascript:/i);
});

test("sanitizeHtmlFragment strips dangerous form and namespaced URL attributes too", () => {
  const html = `
    <button formaction="javascript:alert('xss')">Run</button>
    <a xlink:href="data:text/html,<script>alert(1)</script>">Vector</a>
    <iframe srcdoc="<script>alert('xss')</script>"></iframe>
    <p>Safe fallback</p>
  `;

  const result = sanitizeHtmlFragment(html);

  assert.match(result, /<p>Safe fallback<\/p>/);
  assert.doesNotMatch(result, /\sformaction\s*=/i);
  assert.doesNotMatch(result, /\sxlink:href\s*=/i);
  assert.doesNotMatch(result, /\ssrcdoc\s*=/i);
  assert.doesNotMatch(result, /data:\s*text\/html/i);
  assert.doesNotMatch(result, /javascript:/i);
});
