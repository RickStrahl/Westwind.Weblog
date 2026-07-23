/*
Language: HTTP
Author: Rick Strahl <rstrahl@west-wind.com>
Description: HTTP request/response highlighting with header/value parsing and JSON/XML body highlighting.
*/

function httpLanguage(hljs) {
  var HTTP_METHODS =
    "GET POST PUT PATCH DELETE HEAD OPTIONS TRACE CONNECT";

  var KNOWN_HEADER_VALUES =
    "application/json application/problem+json application/xml text/xml text/html text/plain multipart/form-data " +
    "application/x-www-form-urlencoded charset=utf-8 utf-8 bearer basic digest " +
    "gzip deflate br chunked keep-alive close no-cache max-age";

  var KNOWN_HEADER_VALUES_RE =
    /(application\/json|application\/problem\+json|application\/xml|text\/xml|text\/html|text\/plain|multipart\/form-data|application\/x-www-form-urlencoded|charset=utf-8|utf-8|bearer|basic|digest|gzip|deflate|br|chunked|keep-alive|close|no-cache|max-age)/i;

  return {
    name: "http",
    aliases: ["http", "https", "resthttp", "rest"],
    case_insensitive: true,
    contains: [
      {
        begin: /^(GET|POST|PUT|PATCH|DELETE|HEAD|OPTIONS|TRACE|CONNECT)\b/,
        end: /$/,
        keywords: HTTP_METHODS,
        contains: [
          {
            className: "link",
            begin: /\/(?:[^\s]*)?(?=\s+HTTP\/\d(?:\.\d+)?$)/
          },
          {
            className: "meta",
            begin: /HTTP\/\d(?:\.\d+)?$/
          }
        ]
      },
      {
        begin: /^HTTP\/\d(?:\.\d+)?/,
        end: /$/,
        contains: [
          {
            className: "meta",
            begin: /^HTTP\/\d(?:\.\d+)?/
          },
          {
            className: "number",
            begin: /\b\d{3}\b/
          },
          {
            className: "string",
            begin: /\b\d{3}\s+/,
            end: /$/,
            excludeBegin: true
          }
        ]
      },
      {
        begin: /^[A-Za-z][A-Za-z-]*(?=\s*:)/,
        end: /$/,
        returnBegin: true,
        contains: [
          {
            className: "attribute",
            begin: /^[A-Za-z][A-Za-z-]*/
          },
          {
            begin: /:\s*/,
            end: /$/,
            contains: [
              {
                className: "punctuation",
                begin: /:/,
                relevance: 0
              },
              {
                className: "link",
                begin: /\bhttps?:\/\/[^\s,;]+/i
              },
              {
                // Secondary semantic highlight for known common HTTP header values.
                className: "built_in",
                begin: KNOWN_HEADER_VALUES_RE
              },
              {
                className: "number",
                begin: /\b\d+\b/
              },
              {
                className: "keyword",
                beginKeywords: KNOWN_HEADER_VALUES
              },
              hljs.APOS_STRING_MODE,
              hljs.QUOTE_STRING_MODE
            ]
          }
        ]
      },
      {
        // JSON-like body tokenization for payload readability in older hljs runtimes.
        className: "attr",
        begin: /"(?:\\.|[^"\\])*"(?=\s*:)/
      },
      {
        className: "string",
        begin: /"(?:\\.|[^"\\])*"/
      },
      {
        className: "number",
        begin: /\b\d+(?:\.\d+)?\b/
      },
      {
        className: "punctuation",
        begin: /[{}\[\],:]/,
        relevance: 0
      },
      {
        className: "meta",
        begin: /<\/?[A-Za-z][^>]*>/
      }
    ]
  };
}

hljs.registerLanguage("http", httpLanguage);
