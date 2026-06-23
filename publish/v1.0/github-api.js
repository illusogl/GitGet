#!/usr/bin/env node
/**
 * GitGet GitHub API Bridge
 *
 * Usage: node scripts/github-api.js <method> <endpoint> [params_json] [token]
 *
 * Prints JSON result to stdout.
 * Prints JSON error to stderr.
 */

const https = require("https");
const http = require("http");

const BASE_URL = "api.github.com";

main().catch((err) => {
  console.error(
    JSON.stringify({ error: err.message ?? String(err) })
  );
  process.exit(1);
});

async function main() {
  const args = process.argv.slice(2);

  const method = (args[0] ?? "GET").toUpperCase();
  const endpoint = args[1];
  const paramsJson = args[2] ?? "{}";
  const token = args[3] ?? "";

  if (!endpoint) {
    console.error(JSON.stringify({ error: "endpoint is required" }));
    process.exit(1);
  }

  let params;
  try {
    params = JSON.parse(paramsJson);
  } catch {
    params = {};
  }

  let urlPath = endpoint;
  const queryParts = [];

  for (const [key, value] of Object.entries(params)) {
    if (value !== undefined && value !== null) {
      queryParts.push(
        encodeURIComponent(key) + "=" + encodeURIComponent(String(value))
      );
    }
  }

  if (queryParts.length > 0) {
    urlPath = urlPath + "?" + queryParts.join("&");
  }

  const options = {
    hostname: BASE_URL,
    path: urlPath,
    method: method,
    headers: {
      "Accept": "application/vnd.github+json",
      "User-Agent": "GitGet/1.0",
      ...(token ? { Authorization: "Bearer " + token } : {}),
    },
  };

  const result = await httpRequest(options);
  process.stdout.write(JSON.stringify(result));
}

function httpRequest(options) {
  return new Promise((resolve, reject) => {
    const req = https.request(options, (res) => {
      const chunks = [];
      res.on("data", (chunk) => chunks.push(chunk));
      res.on("end", () => {
        const data = Buffer.concat(chunks).toString("utf8");
        try {
          const parsed = JSON.parse(data);

          // Parse rate-limit headers
          parsed.__rateLimit = {
            limit: parseInt(res.headers["x-ratelimit-limit"] ?? "0"),
            remaining: parseInt(
              res.headers["x-ratelimit-remaining"] ?? "0"
            ),
            reset: parseInt(res.headers["x-ratelimit-reset"] ?? "0"),
          };

          // Attach link header for pagination
          const linkHeader = res.headers["link"];
          if (linkHeader) {
            parsed.__pagination = linkHeader;
          }

          resolve(parsed);
        } catch {
          resolve({
            __raw: data,
            __status: res.statusCode,
            __rateLimit: {
              limit: parseInt(res.headers["x-ratelimit-limit"] ?? "0"),
              remaining: parseInt(
                res.headers["x-ratelimit-remaining"] ?? "0"
              ),
              reset: parseInt(res.headers["x-ratelimit-reset"] ?? "0"),
            },
          });
        }
      });
    });

    req.on("error", reject);
    req.on("timeout", () => {
      req.destroy();
      reject(new Error("Request timed out"));
    });

    req.setTimeout(30000);
    req.end();
  });
}