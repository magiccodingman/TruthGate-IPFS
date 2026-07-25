# TruthGate Documentation


> <a type="button" href="https://github.com/TruthOrigin/TruthGate-IPFS" target="_blank" rel="noopener" class="mud-button-root mud-icon-button mud-inherit-text hover:mud-inherit-hover mud-ripple mud-ripple-icon" _bl_3=""><span class="mud-icon-button-label"><!--!--><svg class="mud-icon-root mud-svg-icon mud-icon-size-medium" focusable="false" viewBox="0 0 24 24" aria-hidden="true" role="img"><!--!--><path d="M12 .3a12 12 0 0 0-3.8 23.4c.6.1.8-.3.8-.6v-2c-3.3.7-4-1.6-4-1.6-.6-1.4-1.4-1.8-1.4-1.8-1-.7.1-.7.1-.7 1.2 0 1.9 1.2 1.9 1.2 1 1.8 2.8 1.3 3.5 1 0-.8.4-1.3.7-1.6-2.7-.3-5.5-1.3-5.5-6 0-1.2.5-2.3 1.3-3.1-.2-.4-.6-1.6 0-3.2 0 0 1-.3 3.4 1.2a11.5 11.5 0 0 1 6 0c2.3-1.5 3.3-1.2 3.3-1.2.6 1.6.2 2.8 0 3.2.9.8 1.3 1.9 1.3 3.2 0 4.6-2.8 5.6-5.5 5.9.5.4.9 1 .9 2.2v3.3c0 .3.1.7.8.6A12 12 0 0 0 12 .3"></path></svg></span></a> Drop a star on [GitHub](https://github.com/TruthOrigin/TruthGate-IPFS) to show some love (or to submit a ticket / discussion)

> <a type="button" href="https://discord.gg/vN6XNQNufn" target="_blank" rel="noopener" class="mud-button-root mud-icon-button mud-inherit-text hover:mud-inherit-hover mud-ripple mud-ripple-icon" _bl_2=""><span class="mud-icon-button-label"><!--!--><svg class="mud-icon-root mud-svg-icon mud-icon-size-medium" focusable="false" viewBox="0 0 24 24" aria-hidden="true" role="img"><!--!--><path d="M19,6.75A9.2017,9.2017,0,0,0,14.625,5l-.2135.4274a8.4519,8.4519,0,0,1,4.151,2.1976A12.87,12.87,0,0,0,12,5.875a12.87,12.87,0,0,0-6.5625,1.75,8.6339,8.6339,0,0,1,4.151-2.1976L9.375,5A9.1429,9.1429,0,0,0,5,6.75S2.76,9.9989,2.375,16.375A8.2659,8.2659,0,0,0,8.0625,19l.7175-.9555A8.7654,8.7654,0,0,1,5,15.5a11.032,11.032,0,0,0,7,2.1875A11.032,11.032,0,0,0,19,15.5a8.7609,8.7609,0,0,1-3.78,2.5445L15.9375,19a8.2659,8.2659,0,0,0,5.6875-2.625C21.24,9.9989,19,6.75,19,6.75ZM9.1562,14.625a1.6511,1.6511,0,0,1-1.5312-1.75,1.6511,1.6511,0,0,1,1.5312-1.75,1.6511,1.6511,0,0,1,1.5313,1.75A1.6511,1.6511,0,0,1,9.1562,14.625Zm5.6876,0a1.6511,1.6511,0,0,1-1.5313-1.75,1.6511,1.6511,0,0,1,1.5313-1.75,1.6511,1.6511,0,0,1,1.5312,1.75A1.6511,1.6511,0,0,1,14.8438,14.625Z"></path></svg></span></a> Join the [Discord](https://discord.gg/vN6XNQNufn) server

> <a type="button" href="https://x.com/MagicCodingMan" target="_blank" rel="noopener" class="mud-button-root mud-icon-button mud-inherit-text hover:mud-inherit-hover mud-ripple mud-ripple-icon" _bl_4=""><span class="mud-icon-button-label"><!--!--><svg class="mud-icon-root mud-svg-icon mud-icon-size-medium" focusable="false" viewBox="0 0 24 24" aria-hidden="true" role="img"><!--!--><path d="M 18.625 1.985 L 22.157 1.985 L 14.403 10.813 L 23.461 22.788 L 16.355 22.788 L 10.79 15.51 L 4.417 22.788 L 0.885 22.788 L 9.099 13.346 L 0.427 1.985 L 7.71 1.985 L 12.738 8.636 L 18.625 1.985 Z M 17.39 20.714 L 19.346 20.714 L 6.683 3.981 L 4.58 3.981 L 17.39 20.714 Z"></path></svg></span></a> Follow me on [Twitter/X](https://x.com/MagicCodingMan)


<br/>

Support TruthGate by pinning the IPNS key or the latest CID:

<section id="truthgate-meta" data-domain="truthgate.io">
  <p>IPNS Peer ID:</p>
  <pre><code id="ipnsPeerIdCode">loading…</code></pre>

  <p style="margin-top:0.75rem;">CIDv0:</p>
  <pre><code id="cidV0Code">loading…</code></pre>

  <p id="truthgate-docs-line" style="margin-top:0.75rem;">
    These values are fetched live from TruthGate’s API endpoints for this site
    (latest at the time of load). Learn how to use this feature yourself in the
    <a href="./docs/site-publishing"
       target="_blank"
       rel="noopener noreferrer"
       class="mud-typography mud-link mud-primary-text mud-link-underline-hover mud-typography-caption mud-treeview-item-label active-section-link">
       Site Publishing Docs
    </a>.
  </p>
</section>

<script>
(() => {
  // optional: wait until truthgateApi is present if scripts load out of order
  function waitForApi(maxMs = 3000) {
    const start = Date.now();
    return new Promise((resolve, reject) => {
      (function check() {
        if (window.truthgateApi?.GetDomainIpnsAt && window.truthgateApi?.GetDomainCidAt) {
          resolve(window.truthgateApi);
        } else if (Date.now() - start > maxMs) {
          reject(new Error('truthgateApi not available'));
        } else {
          setTimeout(check, 30);
        }
      })();
    });
  }

  async function renderTruthMeta() {
    const domain = (document.getElementById('truthgate-meta')?.dataset.domain || 'truthgate.io').trim();

    try {
      const api = await waitForApi(); // ensures the namespaced export exists

      const [ipns, cid] = await Promise.all([
        api.GetDomainIpnsAt(domain), // ✅ host-aware, no query string needed
        api.GetDomainCidAt(domain)   // ✅ returns parsed cidV0/cidV1 objects
      ]);

      const ipnsEl = document.getElementById('ipnsPeerIdCode');
      if (ipnsEl) ipnsEl.textContent = ipns?.ipnsPeerId ?? '(not available)';

      const cidV0El = document.getElementById('cidV0Code');
      if (cidV0El) {
        const v0 = cid?.cidV0;
        cidV0El.textContent = v0?.CidStr ?? v0?.Formatted ?? '(not available)';
      }

    } catch (err) {
      const msg = `Error: ${err?.message ?? String(err)}`;
      const ipnsEl = document.getElementById('ipnsPeerIdCode');
      const cidV0El = document.getElementById('cidV0Code');
      if (ipnsEl) ipnsEl.textContent = msg;
      if (cidV0El) cidV0El.textContent = msg;
    }
  }

  if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', renderTruthMeta);
  } else {
    renderTruthMeta();
  }
})();
</script>


---


> **Before you continue:**  
> If you haven’t set up TruthGate yet, please start with the [Setup Guide](./docs/setup).  
> This page assumes you already have TruthGate installed and running.



## Getting Started

Once TruthGate is installed, you can access your personal gateway at:

```
https://YOUR_IP
```

Make sure you’ve changed the **default password**.  
If you prefer, you can also point a domain (IPv4 or IPv6) at your server, but using the raw IP is perfectly fine.

---

## Secure IPFS Access

TruthGate allows you to use your node like a true **private, secure gateway**:

- `https://YOUR_IP/ipfs/CID`
    
- `https://YOUR_IP/ipns/KEY`
    

These behave just like the public gateways you may be used to — except they’re **isolated, hardened, and under your control**.

---

## The Web UI

From the top navigation menu, click **“Web UI”**.  
This will open the familiar **IPFS Node GUI** (the same one from the desktop app), but safely tunneled through TruthGate.

You can:

- Browse your node
    
- Create folders
    
- Upload files
    
- Pin and manage content
    

Everything works exactly as you’d expect, with the added security layer of TruthGate.

---

## The Dashboard

Your **Dashboard** gives you a quick overview of your node:

- TruthGate + IPFS version
    
- Number of peers
    
- Repo storage used
    
- Bandwidth consumption
    
- (and more stats coming soon)
    

---

## Pin Management

In the navigation bar, you’ll see **Pinned** then two options:

### Pinned CIDs

Manage pinned CIDs quickly and easily.  
Add or remove entries with a simple interface.

![Alt text](/images/demo/pinned-dark.webp)


---

### Pinned IPNS

This page is special, it allows you to **pin entire IPNS keys**, not just static CIDs.

Features include:

- Automatic re-pinning of updated IPNS entries
    
- Full support for the [TruthGate Pointer Protocol](./docs/tgp)
    

![Alt text](/images/demo/pinned-ipns-dark.webp)

---

## Settings

The **Settings** dropdown contains two critical pages for managing access and automation.

---

### Users

On the **Users** page you can add new accounts, manage authentication, and update passwords.  
This ensures that only authorized people can access your TruthGate instance.

![Alt text](/images/demo/users-dark.webp)

---

### API

The **API** page allows you to generate and manage API keys for programmatic access.

- **Main view**:  
![Alt text](/images/demo/api-dark.webp)

- **Key generation popup**:  
![Alt text](./images/demo/api-add-dark.webp)



Once you’ve created an API key, you can call your node like so:

```
https://YOUR_IP/api/v0/YOUR_CALL
```

You can pass your key in one of two ways:

- **Header**:  
    `X-API-Key: YOUR_KEY`
    
- **Query string**:  
    `?api_key=YOUR_KEY` or `?key=YOUR_KEY`
    

> API keys are **only shown once**. They are stored internally as a secure hash, so they cannot be recovered later.  
> Save them immediately in a secure, encrypted vault (e.g., KeePass, Bitwarden).


---

## Domains: The Powerhouse

The **Domains** page is where TruthGate truly shines.

This is the **bridge between Web2 and Web3 hosting**:

- Publish websites directly to IPFS
    
- Seamlessly bind domains for easy Web2 access
    
- Enable hybrid decentralized hosting
    

If you’re a web developer (or want to host a site with TruthGate), head to:  
[Site Publishing Guide](./docs/site-publishing)

---

## Welcome to TruthGate

TruthGate is more than just a secure gateway, it’s part a family of protocols and tools designed to make **Web3 mainstream, secure, and usable**.
