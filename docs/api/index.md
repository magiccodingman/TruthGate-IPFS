
# Public Domain API (read-only)

TruthGate exposes two **anonymous** (no auth required) endpoints under your domain to help apps, tools, and checkers discover the **current deploy CIDs** and **IPNS status** for a published site.

> **Base path:** `https://YOUR_DOMAIN/api/truthgate/v1`

### Endpoints

|Method|Path|What it returns|
|--:|---|---|
|GET|`/GetDomainCid`|The site’s current **root CID** in **v1 (base32)** and **v0 (base58btc)**|
|GET|`/GetDomainIpns`|IPNS metadata for this domain + TGP (TruthGate Pointer) status|

> These endpoints infer the **domain from the Host header** (i.e., the domain you call them on). No query/body params needed.  
> Call them on the **actual site domain** you published (or its configured IPNS host), not the raw server IP.

---

### `GET /GetDomainCid`

Returns the latest deploy’s resolved site **CID** (folder CID of `/production/sites/<leaf>`), normalized to both versions.

**Request**

```
GET https://YOUR_DOMAIN/api/truthgate/v1/GetDomainCid
```

**Sample response**

```json
{
  "domain": "yourdomain.com",
  "cidV0": "QmExampleBase58Btc...",
  "cidV1": "bafybeiexamplebase32..."
}
```

**Notes**

- Tries exact and lowercased leaf names in MFS:
    
    - `/production/sites/<leaf>`
        
    - `/production/sites/<leaf-lower>`
        
- Formats both **CIDv1 (base32)** and **CIDv0 (base58btc)** for maximum compatibility.
    

**Errors**

- `400` – `{"error":"No host header present in request"}` or `{"error":"Invalid host"}`
    
- `404` – `{"error":"site not found","searched":["/production/sites/<leaf>","/production/sites/<leaf-lower>"]}`
    

---

### `GET /GetDomainIpns`

Returns **IPNS + TGP** info for the domain as configured in TruthGate.

**Request**

```
GET https://YOUR_DOMAIN/api/truthgate/v1/GetDomainIpns
```

**Sample response**

```json
{
  "domain": "yourdomain.com",
  "ipnsPeerId": "k51qzi5uqu5d...",
  "tgpCid": "bafybeiexampletgp...",
  "currentCid": "bafybeicurrentpointer...",
  "lastPublishedCid": "bafybeilastpublished...",
}
```

**Fields**

- `ipnsPeerId` – The **key’s PeerID** (`k51...`). If the key name exists but the PeerID isn’t known, TruthGate will attempt to resolve it.    
    
- `tgpCid` – The current **CID of the TGP folder**.
    
- `currentCid` – The **active target CID** from `tgp.json` (the one clients should load).
    
- `lastPublishedCid` – The most recent **site root CID** your domain published (from config).
    

**Errors**

- `400` – `{"error":"No host header present in request"}` or `{"error":"Invalid host"}`
    
- `404` – `{"error":"Domain not found in config"}`
    

> TGP status is cached briefly for speed; after a publish, values may take a moment to reflect. `GetDomainCid` uses a no-cache stat for the site folder then formats the CID.

---

### Practical examples

**Fetch the current deploy CIDs**

```bash
curl -s https://YOUR_DOMAIN/api/truthgate/v1/GetDomainCid | jq
```

**Fetch IPNS + TGP info**

```bash
curl -s https://YOUR_DOMAIN/api/truthgate/v1/GetDomainIpns | jq
```

**In a browser**  
You can also just visit the endpoints directly (handy for external monitors or a quick sanity check).


Personally I utilize these calls to easily check if the current user is utilizing Web3 and if their current version is out of date or not. I've done tricks like that on multiple sites before, but I wanted TruthGate to have this capability to make my life easier moving forward!


---

### Security model


> These endpoints are marked **AllowAnonymous** and are intended to be **public, read-only metadata**. They reveal no private keys, only deployment pointers (CIDs, IPNS PeerID, TGP status).
> 
> Extremely aggressive caching occurs with these API calls to prevent abuse.

---

### When to use which

- Use **`GetDomainCid`** when you need the **current site folder CID** in both v0/v1 formats (e.g., for gateway URLs, debugging, or CID-aware clients).
    
- Use **`GetDomainIpns`** when you need the **IPNS identity** and **TGP pointer** info (e.g., to confirm the active pointer `currentCid`, or to script cross-node pinning).
    

---

## Compression Notes

When publishing your site, many compilers will also generate pre-compressed files like:

- `.gz` (Gzip)
    
- `.br` (Brotli)
    

⚠️ **Do not publish these to IPFS.**

IPFS in Web3 mode cannot directly serve pre-compressed assets — and uploading them just wastes space.

TruthGate automatically handles **compressing and serving** your files efficiently. That means:

- You publish the raw files (`.html`, `.css`, `.js`, images, etc.)
    
- TruthGate delivers them compressed on-the-fly
    
- You save storage space and bandwidth
    


> Keep your build clean, let TruthGate handle compression for you.

---


## Caching & Update Delays

TruthGate is designed with **very aggressive caching** to maximize speed and reduce unnecessary strain on your node. This means that after you make changes or publish new content, **updates may not appear immediately**.

In most cases, updates propagate quickly, but depending on your environment, caches may take **30 minutes or more** to refresh before the latest content is visible. This is normal behavior and not a bug.

If you need to bypass caching instantly, you can clear your local browser cache or use a direct IPFS/IPNS resolution to confirm that the new content has been published correctly.

If you really need an instant update. After the IPNS update occurs (usually happens after 1-2 minutes after a publish), just go ahead and run a restart on the TruthGate service.

---

## Final Thoughts

With the Domains page, you can now:

- Bind **Web2 domains** directly to your decentralized IPFS/IPNS sites
    
- Serve as your own **personal edge gateway**
    
- Gain redundancy, archiving, and true **Web3 presence**
    

Welcome to Web3, powered by **TruthGate**. 🚀
