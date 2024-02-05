5coy-archive
============

Source code for the 5coy-archive.org.uk web site.

---

Notes:

* The site is fronted by Cloudflare for caching
* The archive UI is built as a Blazor WASM application served by an Azure Static WebApp instance.
* Markdown and json data is static content served from the SWA instance.
* PDFs are served from a separate url (https://media.5coy-cmc.org.uk) that fronts an Amazon S3 bucket.
* A Cloudflare Edge Worker pulls from the AWS S3 bucket origin in response to requests for /media/*

