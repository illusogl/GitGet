#!/usr/bin/env node
/**
 * GitGet GitHub OAuth Bridge (Device Flow)
 *
 * Usage:
 *   node github-oauth.js request-device-code
 *   node github-oauth.js poll-token <device_code>
 *
 * Prints JSON result to stdout.
 * Prints JSON error to stderr.
 */

const https = require("https");

const CLIENT_ID = "Ov23liZeGHY2vsaN4gyq";
const DEVICE_CODE_URL = "https://github.com/login/device/code";
const ACCESS_TOKEN_URL = "https://github.com/login/oauth/access_token";

const args = process.argv.slice(2);
const command = args[0];

if (!command) {
  console.error(JSON.stringify({ error: "command is required" }));
  process.exit(1);
}

Main().catch((err) => {
  console.error(JSON.stringify({ error: err.message ?? String(err) }));
  process.exit(1);
});

async function Main() {
  switch (command) {
    case "request-device-code":
      await RequestDeviceCode();
      break;
    case "poll-token":
      await PollToken(args[1]);
      break;
    default:
      console.error(JSON.stringify({ error: `Unknown command: ${command}` }));
      process.exit(1);
  }
}

async function RequestDeviceCode() {
  const body = new URLSearchParams({
    client_id: CLIENT_ID,
    scope: "user repo",
  }).toString();

  const result = await httpPost(DEVICE_CODE_URL, body);
  const data = JSON.parse(result);

  // Return normalized response
  process.stdout.write(JSON.stringify({
    device_code: data.device_code,
    user_code: data.user_code,
    verification_uri: data.verification_uri,
    expires_in: data.expires_in,
    interval: data.interval,
  }));
}

async function PollToken(deviceCode) {
  if (!deviceCode) {
    console.error(JSON.stringify({ error: "device_code is required" }));
    process.exit(1);
  }

  const body = new URLSearchParams({
    client_id: CLIENT_ID,
    device_code: deviceCode,
    grant_type: "urn:ietf:params:oauth:grant-type:device_code",
  }).toString();

  const result = await httpPost(ACCESS_TOKEN_URL, body);
  const data = JSON.parse(result);

  if (data.error) {
    process.stdout.write(JSON.stringify({
      error: data.error,
      error_description: data.error_description || "",
    }));
  } else {
    process.stdout.write(JSON.stringify({
      access_token: data.access_token,
      token_type: data.token_type,
      scope: data.scope,
    }));
  }
}

function httpPost(url, body) {
  return new Promise((resolve, reject) => {
    const parsed = new URL(url);
    const controller = new AbortController(); // not directly used in http.request, use timeout

    const options = {
      hostname: parsed.hostname,
      path: parsed.pathname,
      method: "POST",
      headers: {
        "Accept": "application/json",
        "Content-Type": "application/x-www-form-urlencoded",
        "User-Agent": "GitGet/1.0",
        "Content-Length": Buffer.byteLength(body),
      },
      timeout: 10000, // 10 second timeout
    };

    const req = https.request(options, (res) => {
      const chunks = [];
      res.on("data", (chunk) => chunks.push(chunk));
      res.on("end", () => {
        const data = Buffer.concat(chunks).toString("utf8");
        resolve(data);
      });
    });

    req.on("timeout", () => {
      req.destroy();
      reject(new Error("Request timed out after 10 seconds"));
    });

    req.on("error", reject);
    req.write(body);
    req.end();
  });
}