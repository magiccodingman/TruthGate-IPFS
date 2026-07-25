# TruthGate Publishing

The **Domains** tab in the navigation menu is where TruthGate becomes more than a secure IPFS gateway, it becomes your **publishing powerhouse**.

![Domains page](./images/demo/domains-dark.webp)

---

## Adding Domains

You can add your own domains (Web2 **and** Web3).

- TruthGate can automatically issue **trusted SSL certificates** via **Let’s Encrypt**.
    
- If you’re using **Cloudflare** for SSL and CDN caching (highly recommended), simply disable the auto-SSL option in TruthGate.
    
    - TruthGate will still issue a local SSL cert.
        
    - On Cloudflare, set the proxy SSL mode to **“Full”** and it will connect just fine.
        

To connect domains:

- Add a DNS **A record** → point to your server’s IPv4
    
- Add a DNS **AAAA record** → point to your server’s IPv6
    

---

## Built-in IPNS + TGP

When publishing, TruthGate automatically uses **IPNS** with the [TruthGate Pointer Protocol (TGP)](./docs/tgp).

- Normal IPNS is notoriously slow.
    
- TruthGate + TGP make IPNS **lightning fast** and legally cleaner.
    

For maximum performance, configure a **Wildcard Host** in **Settings → General**:

1. Create DNS records:
    
    - `A` and `AAAA` records for `*.ipns` → point to your TruthGate IPs.
        
    - Example: `*.ipns.truthgate.io`
        
2. In TruthGate, set your Wildcard Host to `ipns.truthgate.io`
    
3. (Optional but recommended) Enable **TruthGate Managed SSL**.
    

Save your settings, and now every domain you publish will have a dedicated, super-fast **IPNS domain** automatically linked.

---

## Drag-and-Drop Publishing

When you click **Publish**, you’ll see this upload interface:

![Publish page](./images/demo/domains-publish-dark.webp)

Simply:

1. Build your static site (e.g., a Blazor or React app).
    
2. Drag and drop your `wwwroot` folder into the upload box.
    

TruthGate will handle the IPFS/IPNS deployment for you. 🎉

> Uploads are limited by browser constraints (file count & total size). For huge deployments (tens of thousands of files, or very large builds), use the **Publish API** instead.

---

## Publishing via API

TruthGate exposes a special API for large deployments:

```
POST https://YOUR_IP/api/truthgate/v1/admin/publish
```

Here's a Powershell script I personally use to upload my site:

```powershell
$apiKey = "Your_Key"
$domain = "Your_Domain"
$UserIP = "Your_IP"
$root  = "Full_Path_To_wwwroot"

$files = Get-ChildItem $root -File -Recurse

$url    = "https://$UserIP/api/truthgate/v1/admin/$domain/publish"

$args = @(
  "-k", "-X", "POST",
  $url,
  "-H", "X-API-Key: $apiKey"
)

foreach ($f in $files) {
  # compute path relative to $root and force forward slashes
  $rel = $f.FullName.Substring($root.Length).TrimStart('\','/').Replace('\','/')
  # IMPORTANT: we attach the file from the full path, but we override the transmitted filename
  $args += @("-F", "files[]=@$($f.FullName);filename=$rel")
}

# fire!
curl.exe @args

```

This will publish every file in your `wwwroot` folder to your domain.

---

## Backup & Import

After your first publish, you’ll see a **Backup** button.  
This generates a secure file containing your **IPNS key + peer identity**.

⚠️ Important notes:

- This is **not** a backup of your site’s files — only the **IPNS identity**.
    
- If you lose this key, there is **no recovery**.
    
- Store it in a safe, encrypted vault.
    

TruthGate will prompt you to password-protect the backup for safety.

If needed, you can later use the **Import** button to restore a backed-up domain.

---

## Redundancy & Best Practices

Want redundancy? Deploy multiple TruthGate nodes:

- Use the **IPNS pinning feature** to replicate your published domain across multiple nodes.
    
- This increases availability and improves site performance.
    

However:

- Always choose **one primary TruthGate** for publishing.
    
- Secondary nodes should only pin, not publish, to avoid conflicts.

---

It's highly suggested you read and understand the API docs. It's very helpful to utilize the provided API endpoints for your website.
<br/>
[Click here to check out the API documentation](./docs/api)