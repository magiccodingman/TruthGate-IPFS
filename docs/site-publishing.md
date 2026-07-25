# Site publishing

TruthGate turns a static build directory into an IPFS-hosted site that can be reached by CID, optionally updated through IPNS/TGP, and served through one or more HTTPS domains.

![Publishing a mapped domain](../content/images/demo/domains-publish-dark.webp)

## What can be published

TruthGate is designed for files that can be served without an application server:

- HTML, CSS, and JavaScript
- images, fonts, and downloadable assets
- WebAssembly
- Blazor WebAssembly output
- React, Vue, Svelte, and other SPA production builds
- static documentation sites
- pre-rendered application output

A server-rendered application that requires its own backend process is not a static site merely because it has a frontend.

## Publishing model

A publication connects four layers:

1. **Site files** — the current static build.
2. **Site CID** — the immutable IPFS root produced from those files.
3. **Mutable identity** — optional IPNS and TGP metadata pointing to the current CID.
4. **Domain mapping** — optional HTTP routing and certificate configuration.

The CID remains independently usable even when a domain or IPNS identity is added.

## Basic workflow

1. Build the site for production.
2. Confirm that the build output uses relative or deployment-compatible paths.
3. Open the Domains area in TruthGate.
4. Create or select the domain configuration.
5. Upload or publish the build output.
6. Wait for the publication job to finish.
7. Record the resulting CID.
8. Verify the CID directly.
9. If configured, verify IPNS/TGP.
10. Verify the HTTPS domain.

## Domain behavior

A mapped domain is not the management portal. Requests for that host are routed to the site's published IPFS content.

An IP address or unmapped management host serves the authenticated TruthGate application.

Read [Request routing](concepts/request-routing.md) before adding unusual redirects, wildcard mappings, or multiple project hosts.

## TLS

TruthGate uses a self-signed fallback certificate for IP addresses and unknown hosts. Configured domains can receive ACME certificates.

For issuance to succeed:

- DNS must point to the server.
- Public TCP port `80` must reach TruthGate for HTTP-01 challenges.
- Public TCP port `443` must reach TruthGate for HTTPS.
- Another reverse proxy must not consume the challenge unless it forwards it correctly.

Use ACME staging while testing repeated certificate changes.

## SPA routing

A single-page application often expects unknown paths to return `index.html`. TruthGate's mapped-domain pipeline supports SPA-oriented fallback behavior.

That fallback should not hide genuinely missing assets. Verify deep links and direct asset URLs separately.

## CID verification

Always verify the immutable output before debugging a domain:

```text
https://YOUR_TRUTHGATE_HOST/ipfs/<CID>/
```

An authenticated gateway request may require a login session or API key depending on the route and current configuration.

From another IPFS node, a stronger validation is:

```bash
ipfs cat /ipfs/<CID>/index.html
```

This distinguishes publication problems from DNS, TLS, or domain-routing problems.

## IPNS and watched pins

IPNS gives a stable identity whose target can change. TruthGate can also watch an IPNS name and keep its current target pinned locally.

Publishing to IPNS and subscribing to IPNS are related but distinct operations:

- **Publishing** updates an identity you control.
- **Watching** resolves an identity and maintains its targets locally.

See [Pinning, IPNS, and publishing](concepts/pinning-ipns-and-publishing.md).

## TGP publication

A TGP publication points an IPNS identity at a tiny directory containing `tgp.json`, optional browser fallback content, and an optional legal notice. The `current` field identifies the active site CID.

Read the complete [TGP publisher workflow](tgp/publisher-workflow.md). Do not interpret TGP as a mechanism for deleting copies held by other nodes.

## Public metadata

A published domain can expose its current CID and IPNS/TGP state through read-only metadata endpoints. These are useful for:

- deployment verification;
- freshness checks;
- monitoring;
- client-side discovery;
- cross-node pinning automation.

See [Domain metadata API](api/domain-metadata.md).

## Compression

Do not assume that compiler-generated `.br` or `.gz` files should always be removed. Compression behavior depends on the current publishing and serving implementation and on how the application references those files.

Before documenting a framework-specific rule, test the exact production output through both the mapped HTTPS domain and an IPFS path. The repository documentation intentionally avoids the old universal claim that all precompressed output is unusable on IPFS.

## Publication checklist

- [ ] The build output is static.
- [ ] `index.html` exists at the intended root.
- [ ] Direct CID access works.
- [ ] Deep SPA routes work.
- [ ] Required assets return successful responses.
- [ ] Domain DNS reaches the correct host.
- [ ] Certificate issuance succeeds.
- [ ] IPNS resolves to the expected value, when used.
- [ ] `tgp.json` validates, when used.
- [ ] Public metadata reports the intended CID.
