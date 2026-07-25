# TruthGate

> The Secure, Self-Hosted Edge Gateway IPFS Always Needed, with Logins, API Keys, GUI Control, and Web3 Site Publishing.  
> **[truthgate.io](https://truthgate.io)** for full docs, guides, and live demos.

---

## Features at a Glance

- **Secure User Logins** – Role-based accounts protect access.
- **API Key Management** – Scoped tokens for programmatic control.
- **Clean GUI** – Manage users, domains, and publishing in minutes.
- **`/ipfs/` + `/webui/`** – Native IPFS routes, now behind auth + HTTPS.
- **Auto SSL & Domains** – Custom domains with one-step HTTPS.
- **Drag-and-Drop Publishing** – Deploy WASM, SPAs, and Blazor apps in seconds.
- **Web2/Web3 Hybrid Hosting** – Serve both decentralized and traditional users.
- **Hardened Edge Gateway** – Secure IPFS exposure without the risks.

---

## Docker quick start

TruthGate, Kubo, and the matching `ipfs` CLI run together as one appliance
container. Application state, Kubo repository metadata, and Kubo blocks are
persisted separately by default.

```bash
cp .env.example .env
docker compose up --build -d
docker compose logs truthgate
```

The first-start logs contain a generated password for the `admin` account.
Open `https://localhost`, accept the temporary self-signed fallback certificate,
and change the password.

The appliance configures Kubo as a contributing server by default: DHT server
mode, TCP/QUIC/WebTransport swarm listeners, content providing, automatic
storage sizing, and repository GC are enabled with persistent per-setting
overrides. Inspect the live node with:

```bash
docker exec truthgate truthgate-kubo-status
```

For the full persistence contract, Kubo settings, image update flow,
ARM64/AMD64 publishing, and Docker-based development setup, see
**[DOCKER.md](DOCKER.md)**.

Development with hot reload uses the production definition plus a small
override:

```bash
docker compose -f compose.yaml -f compose.dev.yaml up --build
```

Then open `http://localhost:8080`.

---

## What Is It?

TruthGate is a **secure edge layer for IPFS nodes**.  
Think **Netlify, but for IPFS**, self-hosted, login-protected, and actually yours.

It solves the problems developers hit with IPFS:

- Node exposure risks
- SSL/domain linking headaches
- CLI-only publishing
- Zero protection for `/api` or `/webui` routes
- Reliance on public gateways

[See how it works](https://truthgate.io)

---

## Get Started

Head to **[truthgate.io](https://truthgate.io)** for installation, configuration, and publishing guides.

---

## A Note from the Creator

I built this out of frustration.

I wanted a way to serve Web3-native apps that *actually worked*, securely, reliably, and without selling my soul to centralized hosts. Now it’s real.

If you’ve ever wrestled with IPFS routing, SSL certs, or gateway hacks just to get your site online, **TruthGate is for you.**

---

## Contribute

Pull requests welcome.  
Stars encourage me.  
Issues are sacred.

Let’s fix decentralized hosting together.

[truthgate.io](https://truthgate.io)

---

## Creator

Solo development done by [MagicCodingMan](https://github.com/magiccodingman), but under the TruthOrigin umbrella.
