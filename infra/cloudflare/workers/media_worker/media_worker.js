addEventListener("fetch", (event) => {
    event.respondWith(
      handleRequest(event.request).catch(
        (err) => new Response(err.stack, { status: 500 })
      )
    );
  });
  
  const ORIGIN = 'dxa4dl52uee85.cloudfront.net';
  
  /**
   * @param {Request} request
   * @returns {Promise<Response>}
   */
  async function handleRequest(request) {
    const url = new URL(request.url);
    const pathname = url.pathname;
  
    if (pathname.startsWith("/media")) {
        url.hostname = ORIGIN;
      return fetch(url.toString(), request);
    }
  
    // Otherwise, process request as normal
    return fetch(request);
  }