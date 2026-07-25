# TruthGate Pointer (TGP) Protocol

**Version:** 1.1
**Status:** Open Specification / Production-Ready
**Date:** 2025-08-29
**Homepage:** TruthGate (protocol family)

> **One-liner:** TGP makes an IPNS name behave like a **mutable homepage pointer**—a tiny JSON file at a fixed path that always tells clients which **current** `/ipfs/<cid>` to load. No history breadcrumbs. No accidental seeding. Fast, simple, legally sane.

---

## 1) Purpose & Rationale

Classic IPNS is append-only in spirit: if you ever published an old CID, it can hang around the network forever—and if your node still has those blocks, you might still serve them. That’s operationally messy and a legal headache.

**TruthGate Pointer (TGP)** solves this with a **single tiny file**:

* Keep an IPNS name, but publish only a **pointer file** (`/tgp.json`) at its root.
* That file contains the **current CID** and a timestamp.
* Clients fetch the pointer, then load the current CID. That’s it.

**Design goals**

* **Minimal surface area:** a single file; one fetch → one redirect.
* **No breadcrumbing:** do not surface history by default.
* **Legal posture:** you’re a pointer, not a host of legacy content.
* **Web-friendly:** works on gateways, browsers, IPFS Companion, CLI, anything.

---

## 2) High-Level Overview

1. Publisher builds the site → gets `siteRootCID`.
2. Publisher writes `/tgp.json` pointing to `siteRootCID`.
3. IPNS name is updated to a directory that contains `tgp.json` (and optionally `/index.html` and `/legal.md`).
4. Clients resolving the IPNS name:

   * Fetch `/tgp.json`
   * Read `current`
   * Load `/ipfs/<current>` (or redirect there in a browser)

No segments, no historical lists, no indexes. **TGP is a live signpost.**

---

## 3) Normative Specification

### 3.1 Required File & Location

* A TGP-compliant IPNS name **MUST** publish a JSON document at:

  ```
  /tgp.json
  ```

  (Path is relative to the **root** of the content resolved by the IPNS name.)

### 3.2 JSON Fields (v1)

```json
{
  "tgp": 1,
  "ts": "2025-08-17T21:03:10Z",
  "current": "bafy...siteRootCid",
  "domainName": "Your_Domain.com",
  "legal": "/legal.md"
}
```

* **`tgp`** (integer, REQUIRED): Protocol version. Must be `1` for this spec.
* **`ts`** (string, REQUIRED): ISO-8601 UTC timestamp when the pointer was last updated (e.g., `2025-08-17T21:03:10Z`). Used for freshness.
* **`current`** (string, REQUIRED): Either a bare CID (`bafy…`) **or** a full path `"/ipfs/<cid>"` identifying the active site root.
*  **`domainName`** (string, optional): Primary associated domain name to the IPNS link.
* **`legal`** (string, OPTIONAL): Relative path to a human-readable legal notice, strongly **RECOMMENDED** to be `"/legal.md"` (see §8).

**Unknown fields:** v1 clients **MUST ignore** unknown keys (forward-compatibility).

### 3.3 Client Resolution Rules (MUST/SHOULD)

Given an IPNS name (e.g., via a gateway path or a DNSLink):

1. **Fetch** `/tgp.json` with cache disabled or short-TTL (clients **SHOULD** avoid caching longer than 60 seconds unless instructed otherwise by HTTP headers).
2. **Validate**:

   * JSON parses.
   * `tgp === 1` (or a supported version).
   * `current` exists and is a non-empty string.
3. **Normalize target**:

   * If `current` starts with `/ipfs/`, use as-is.
   * Else, treat it as a CID and construct `/ipfs/<current>`.
4. **Load target**:

   * Browser UX typically **redirects** (`location.replace`) to `/ipfs/<cid>`.
   * Non-browser clients **SHOULD** fetch from `/ipfs/<cid>` directly.
5. **Legal link** (optional):

   * If `legal` is present, clients **MAY** expose a UI affordance linking to it but **MUST NOT** block resolution if missing.

### 3.4 Caching Guidance

* `tgp.json` is tiny and changes on deploys; clients and gateways **SHOULD** use short caching (e.g., 60s).
* The active `/ipfs/<cid>` can be cached normally (it’s immutable).

### 3.5 Error Handling

If `/tgp.json`:

* **404/Network error** → Client **SHOULD** fall back to a last-known good CID (if available) or surface an error.
* **Malformed JSON / missing `current`** → Treat as error; do not guess.

---

## 4) Browser Integration

### 4.1 With `index.html` (Recommended for smooth UX)

Place this file next to `tgp.json` in your IPNS root:

```html
<!doctype html>
<meta charset="utf-8">
<title>Resolving…</title>
<noscript>
  <meta http-equiv="refresh" content="0;url=/ipfs/REPLACE_WITH_LAST_KNOWN_CID">
</noscript>
<script>
(async () => {
  try {
    const res = await fetch('/tgp.json', { cache: 'no-store' });
    if (!res.ok) throw 0;
    const m = await res.json();
    if (!m || !m.current) throw 0;
    const target = m.current.startsWith('/ipfs/') ? m.current : '/ipfs/' + m.current;
    location.replace(target);
  } catch {
    // Last known good CID baked at build time:
    location.replace('/ipfs/REPLACE_WITH_LAST_KNOWN_CID');
  }
})();
</script>
<body>Resolving latest…</body>
```

**Build-time tip:** template `REPLACE_WITH_LAST_KNOWN_CID` to your latest successful deploy so bots and no-JS environments still resolve something.

### 4.2 Without `index.html` (Direct Clients)

* **Browser console**

  ```js
  fetch("https://<gateway>/ipns/<your-name>/tgp.json")
    .then(r => r.json())
    .then(j => window.location.href = "/ipfs/" + (j.current.replace(/^\/ipfs\//, "")));
  ```

* **curl / jq**

  ```bash
  CID=$(curl -fsSL https://<gateway>/ipns/<your-name>/tgp.json | jq -r '.current' | sed 's#^/ipfs/##')
  echo "Current CID: $CID"
  ```

* **IPFS Companion**
  If the user runs Companion, `/ipfs/<cid>` will resolve locally when possible.

---

## 5) Publisher Workflow (Reference)

1. **Build** your site → obtain `siteRootCid`.
2. **Moderation sweep** (if applicable) → de-pin removed content → `ipfs repo gc`.
3. **Write** `/tgp.json`:

   ```json
   {
     "tgp": 1,
     "ts": "2025-08-17T21:03:10Z",
     "current": "bafy...siteRootCid",
     "legal": "/legal.md"
   }
   ```
4. **(Optional)** Write `/legal.md` (see §8).
5. **Add** the folder to IPFS and **publish** the IPNS name to that folder’s CID.

That’s the entire deploy. You never need to publish history or segments.


---

## 6) Gateway / Reverse-Proxy Behavior (Recommended)

Gateways or proxies that serve an IPNS name embracing TGP **SHOULD**:

* Serve `/tgp.json` with **short cache** (≈60s) and forbid stale caching.
* When a request arrives **under the IPNS host/path**, resolve `/tgp.json` and:

  * **Allow** only requests that target the `current` CID found in `tgp.json`.
  * **Reject** attempts to fetch other CIDs via the same IPNS host (prevents using your host to pull old or unrelated content).
* Serve `/legal.md` if present at the advertised path.

This enforcement preserves the pointer-only contract and reduces legal exposure.

---

## 7) Security Considerations

* **No history exposure:** TGP does not surface old CIDs; clients are guided to *one* current CID.
* **Freshness:** Rely on `ts` plus short HTTP caching to avoid stale pointers during IPNS propagation quirks.
* **IPNS guarantees:** IPNS itself tracks sequence and signing; TGP does not duplicate those controls.
* **DoS/poisoning:** Clients should treat malformed `tgp.json` as an error; do not guess a target.
* **Trust model:** Clients trust the IPNS key they resolved (and the gateway they chose). For additional assurance, operators may mirror `/tgp.json` at an HTTPS origin with identical bytes, but that’s outside v1’s scope.

---

## 8) Legal Considerations (Strongly Recommended)

To clarify the pointer-only posture and reduce liability ambiguity across jurisdictions, TGP deployments are **strongly encouraged** to include a legal notice at:

```
/legal.md
```

A ready-to-use, permissively licensed template—**“TruthGate Pointer Legal Notice & License (TGP-LN v1.0)”**—asserts:

* This IPNS name is a **live pointer** only.
* The operator does **not** host or surface legacy content via this IPNS.
* Moderation and takedowns are honored for the **current** CID.
* Third-party caches/mirrors are outside operator control.

> **Recommendation:** Include the `legal` field in `/tgp.json` with the value `"/legal.md"` and publish the provided TGP-LN file verbatim (or with minor formatting/contact additions). See your kit’s `legal.md` template.


To download the suggested legal document crafted, please use:
<a class="mud-typography mud-typography-body1 mud-typography-align-left mud-primary-text" href="./slugs/other/tgp/legal.md" download="legal.md">
  Click To Download The Suggested Legal Document
</a>

---

## 9) JSON Schema (Informative)

For tooling and validation, here’s a JSON Schema (2020-12):

```json
{
  "$schema": "https://json-schema.org/draft/2020-12/schema",
  "$id": "https://truthgate.dev/schemas/tgp.v1.json",
  "title": "TruthGate Pointer v1",
  "type": "object",
  "additionalProperties": true,
  "required": ["tgp", "ts", "current"],
  "properties": {
    "tgp": { "type": "integer", "const": 1 },
    "ts":  { "type": "string", "format": "date-time" },
    "current": { "type": "string", "minLength": 3 },
    "legal": { "type": "string" }
  }
}
```

---

## 10) FAQ (Quick Answers)

* **Q: Why not just use raw IPNS?**
  **A:** Raw IPNS tends to expose/retain history and can cause you to serve old blocks you no longer endorse. TGP restricts the IPNS surface to a single pointer file.

* **Q: Where do I put `index.html`?**
  **A:** At the IPNS root next to `tgp.json`. It’s optional, but makes browsers auto-redirect nicely.

* **Q: What if `/tgp.json` is missing?**
  **A:** TGP clients should treat the IPNS name as **non-compliant**. Gateways can return a helpful error page.

* **Q: Do I need a signature in the JSON?**
  **A:** No. IPNS keys/signatures already authenticate the update. v1 keeps it minimal.

* **Q: Can I keep an audit log?**
  **A:** Yes—off-IPNS. TGP intentionally does not expose history to avoid encouraging legacy content discovery.

* **Q: Does this work with IPFS Companion?**
  **A:** Yes. The redirect points to `/ipfs/<cid>`, which Companion can resolve locally.

---

## 11) Versioning & Extensibility

* TGP uses a numeric `tgp` field.
* Clients **MUST** ignore unknown fields.
* Future versions *may* introduce optional fields (e.g., cache hints, alternates), but v1 remains the canonical minimal spec.

---

## 12) Reference Directory Layout

```
/tgp.json            # REQUIRED (pointer)
/index.html          # OPTIONAL (browser auto-redirect helper)
/legal.md            # STRONGLY RECOMMENDED (legal notice: TGP-LN v1.0)
```

---

## 13) Compliance Checklist

* [ ] `/tgp.json` exists at IPNS root.
* [ ] `tgp` = `1`, `ts` is current, `current` is a valid CID or `/ipfs/<cid>`.
* [ ] Short cache headers for `/tgp.json` (≈60s).
* [ ] (Browser) Optional `index.html` implements one-fetch redirect.
* [ ] **Include** `/legal.md` using the provided TGP-LN template and set `"legal": "/legal.md"` in `/tgp.json`.
* [ ] Gateway/proxy (if present) only serves the `current` CID for this IPNS host.

---

## 14) Example `tgp.json`

```json
{
  "tgp": 1,
  "ts": "2025-08-17T21:03:10Z",
  "current": "bafybeib2n...yourSiteRootCid",
  "legal": "/legal.md"
}
```

---

### Final Word

**TGP** is deliberately small and boring—by design. It gives you **mutable UX on immutable rails**, trims legal exposure, and keeps your deploy flow to “write file → publish IPNS.” Add the **TGP-LN** legal notice at `/legal.md`, and you’ve got a clean, comprehensible contract with users and regulators alike.

