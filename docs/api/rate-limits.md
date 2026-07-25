# TruthGate Rate Limiting & Abuse Protection

**Why this exists.**  
TruthGate is designed to stay online, even when the internet isn’t playing nice. Public endpoints get hammered, keys get fat-fingered, NATs/VPNs share IPs, and botnets love tiny JSON. Our rate limiting and abuse protection system makes TruthGate resilient without making life miserable for real users. It absorbs spikes, distinguishes mistakes from malice, defends against CPU-burning TLS churn, and keeps shared offices from getting blanket-banned.

**What you get.**

- **Protection that scales** from a few rps to sustained floods
    
- **Office/VPN friendliness** via grace rules and tiered limits
    
- **Fairness & transparency**: clear rules, predictable outcomes
    
- **Operational safety nets**: whitelists, unban tools, metrics, automatic cleanup
    

---

## How it’s organized

TruthGate enforces limits in three places:

1. **Admin-protected endpoints** (require an API key; e.g., your admin controllers)
    
2. **Public endpoints** (no key required; your normal public controllers)
    
3. **Gateway routes** (non-controller paths like `/ipfs/**`, `/ipns/**`, `/api/v0/**`), protected via a special `IApplicationBuilder` extension meant to sit at the very top of the pipeline
    

All three share the same **ban list**, **IPv6 /64 graylisting**, **TLS churn detection**, and **whitelist** logic.

---

## Storage & footprint

- **State store:** SQLite (code-first EF Core).
    
- **Location:** the database file is created **beside TruthGate’s main config file** (we resolve the directory from `IConfigService.ConfigPath`).
    
- **Retention:** per-IP minute counters and related data older than **6 months** are purged automatically.
    
- **Performance:** hot counters are kept in memory and **batched** to disk every few seconds; bans and whitelist changes are written immediately.
    

---

## Global concepts (apply everywhere)

### Windows & counters

- **Per-minute counters:** traffic is aggregated by IP into UTC minute buckets.
    
- **Sliding hour total:** last 60 minutes, used to choose public tier limits.
    
- **Sliding 24h window:** last 24 hours, used for admin bad-key thresholds.
    

### Soft bans vs. True bans

- **Soft ban:** you’re blocked, but we keep **counting** for ~10 minutes to see if the behavior cools off or escalates.
    
- **True ban:** you’re blocked and we stop counting (cheap, immediate deny).
    
- We **never disclose** how long a ban lasts in responses.
    

### TLS churn detection

Low-bandwidth, high-CPU attacks often open a brand-new TLS connection for every request. TruthGate detects this pattern:

- If an IP sustains **> 30 new connections/sec** with **≤ 1.2 requests/connection** for ~10s → **soft ban** (with normal escalation if it persists).
    
- This is independent of “requests per minute” limits.
    

### IPv6 /64 graylisting

If abuse pops up across many IPv6 addresses in the same /64:

- **8+ banned IPs in 10 minutes** (or **20+ in 1 hour**) → **graylist the /64** for the **maximum** ban among its members.
    
- Prevents attackers from sidestepping bans by hopping addresses within the same subnet.
    

### Whitelist & unban (admin tools)

- **Whitelist precedence:** whitelisted IPs or IPv6 /64s bypass all limits/bans.
    
- **Manual whitelist:** you can add permanent or time-boxed entries.
    
- **Auto-whitelist (optional):** successful admin auth (valid key) or authenticated gateway use can grant a **7-day auto-whitelist** to ease legitimate heavy use.
    
- **Unban controls:** list bans (paginated), filter, remove by ID/IP/prefix; remove whitelist entries.
    
- These tools are exposed via service methods you can wire behind your admin UI or scripts.
    

---

## 1) Admin-Protected Endpoints (API key required)

**Goal:** never rate-limit valid admins, but stop brute-force, abused keys, and CPU-draining noise.

### Happy path (valid key)

- **No rate limit** by default.
    
- Optional **soft ceiling per key** (e.g., 100 rps sustained / 500 burst) can be enabled to contain a leaked or malfunctioning client. Exceeding it returns `429` and triggers alerts.
    

### Fat-fingers & brute force (invalid/missing keys)

- **Per-IP threshold:** **10 failed key attempts per sliding 24h** across all admin-protected endpoints → **72-hour IP ban**.
    
- **Grace for shared IPs:** if there was a **successful call for (IP, key)** in the last **7 days**, failures from that **same pair** **don’t** count toward the 10/24h threshold. Every success **refreshes** the 7-day grace.
    

### Responses & privacy

- Missing/invalid/suspended key → always **`401`** with the **same** body and timing (no oracles, no hints).
    
- If banned → **`403`** immediately (“Access denied.”).
    

### Escalation during soft ban (admin path)

- **4×** the violated limit in any 10-minute window → post-soft ban escalates to **7 days**.
    
- **10×** → **true ban** of **1825 days** (5 years).
    
- This is targeted at clear, automated abuse—normal users almost never hit these levels.
    

---

## 2) Public Endpoints (no key required)

**Goal:** generous by default, tighter as global load rises.

### Per-IP minute budgets (dynamic tiers)

|Last-hour total (all protected traffic)|Per-IP minute budget|
|---|--:|
|< 2,000 requests|**300/min/IP**|
|≥ 2,000|**200/min/IP**|
|≥ 8,000|**100/min/IP**|
|≥ 16,000|**30/min/IP**|

**Exceeding your per-minute budget:**

- You receive a **15-minute soft ban** and HTTP **`429`** (“Rate limit exceeded.”).
    
- Continued hammering during soft ban triggers the standard **4×** and **10×** escalations (see “Soft vs True”).
    

**Why tiers?**  
As the system gets busier, everyone’s per-IP budget tightens automatically to protect stability and keep latency low for all users.

---

## 3) Gateway Routes (non-controller paths)

These are non-controller endpoints mapped directly in your pipeline, protected by a dedicated extension placed **at the top** of `Program.cs`. It’s designed for **static-ish** or proxied traffic such as:

- `/ipfs/**`
    
- `/ipns/**`
    
- `/api/v0/**`
    

**Intent:** protect your node (and local network) from mass download abuse or proxy storms. It also respects the same **admin key** rules you use elsewhere.

### Per-IP “free + overage” policy

|Measure|Value|Notes|
|---|--:|---|
|Free per-minute|**400 requests/min/IP**|Generous for real users; keeps small bursts smooth.|
|Hourly overage bucket|**3,200 requests/IP/hour**|Sliding last 60 minutes. When a minute exceeds 400, the excess draws down this bucket.|
|Ban on exhaustion|**4 hours**|If you exceed 400/min **and** your hourly overage is at **0**, you’re blacklisted for 4h.|
|4× escalation|**×4 ban duration**|If, after overage is exhausted, you do **≥ 1,600 req/min**, the current ban is quadrupled.|
|10× escalation|**7 days**|If you do **≥ 4,000 req/min** after overage is exhausted, you jump straight to a 7-day ban.|

**Admin awareness on gateway routes**

- If a request carries a **valid admin API key** _or_ the user is otherwise **authenticated**, we:
    
    - Record a **success** for the IP (refreshes admin (IP,key) grace), and
        
    - (Optionally) apply a **7-day auto-whitelist** to that IP, so legitimate heavy usage isn’t throttled.
        

**Denials**

- Banned IPs (or graylisted /64s) are **blocked instantly** by the gateway extension before any expensive work (proxying, disk, backend hops).
    

---

## Escalation rules (all paths, summarized)

|Context|Trigger|Result|
|---|---|---|
|**Public**|Exceed per-minute tier|**Soft ban 15 min** (`429`)|
|**Public / Gateway / Admin (during soft)**|**4×** the violated limit within any 10 min|**Multiply current ban by 4** (Public/Gateway) or **7 days** (Admin invalid key path)|
|**Public / Gateway / Admin (during soft)**|**10×** the violated limit|**True ban**: Public/Gateway → **7 days**; Admin invalid key path → **1825 days**|
|**TLS churn**|>30 new TLS conns/sec & ≤1.2 req/conn for ~10s|**Soft ban 15 min**, escalate if repeated|
|**IPv6 spray**|≥8 banned IPs in /64 in 10 min (or ≥20 in 1h)|**Graylist /64** for the longest member ban|

> **Soft ban vs. True ban:** Soft bans still count requests (to measure escalation). True bans do not; they’re cheap to enforce and return `403` immediately.

---

## Office & VPN friendliness

- **Grace by (IP, key):** a successful admin call within **7 days** shields that **pair** from counting fat-finger mistakes.
    
- **Minute + hourly overage (gateway):** real people click in bursts. The **400/min + 3,200/hour** model absorbs those bursts before any ban is considered.
    
- **Tiered public limits:** everyone gets more conservative limits only when **global** load is high—so quiet hours stay generous.
    
- **IPv6 /64 graylist** targets genuine spray, not one user behind a provider’s NAT64.
    
- **No “key oracle”:** invalid/missing/suspended keys all look the same externally.
    

---

## What users will see

- **Public throttle:** HTTP **429** (`Retry-After: 60`) and a generic “Rate limit exceeded.”
    
- **Bans (any origin):** HTTP **403** (“Access denied.”).
    
- **Admin auth failures:** HTTP **401** with the same response for all invalid/missing/suspended cases.
    
- We **never** disclose remaining quota, ban lengths, or internal thresholds in responses.
    

---

## Operations & Admin

- **Whitelisting:** add/remove IPs or IPv6 /64s (permanent or time-boxed).
    
- **Unban:** remove bans by ID/IP/prefix; filter and page through ban lists.
    
- **Metrics:** counters for allowed/blocked, active bans, whitelist hits, TLS churn triggers, public tier level, and gateway overage usage. Integrate with your metrics stack.
    
- **Housekeeping:** old counters gone after **6 months**; expired bans/whitelists cleaned up; periodic DB maintenance.
    
