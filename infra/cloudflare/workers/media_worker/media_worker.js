import { REDIRECTS } from "./redirects.generated.js";

const ORIGIN = "dxa4dl52uee85.cloudfront.net";

addEventListener("fetch", (event) => {
  event.respondWith(
    handleRequest(event.request).catch(
      (err) => new Response(err.stack, { status: 500 })
    )
  );
});

/**
 * @param {Request} request
 * @returns {Promise<Response>}
 */
async function handleRequest(request) {
  const url = new URL(request.url);
  const pathname = url.pathname;

  // Corpus cleanup (#51): redirect legacy /media/... paths to canonical ones.
  // REDIRECTS is generated at build time from scripts/rename-map.json.
  const canonical = REDIRECTS[pathname];
  if (canonical) {
    const target = new URL(url);
    target.pathname = canonical;
    return Response.redirect(target.toString(), 301);
  }

  if (pathname.startsWith("/media")) {
    url.hostname = ORIGIN;
    return fetch(url.toString(), request);
  }

  return fetch(request);
}
