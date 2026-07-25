# TruthGate Beginner’s VPS On‑Ramp (Ubuntu + PuTTY/SSH)

_A friendly, hand-holding primer for absolute VPS newcomers. After this, you’ll be ready to follow the TruthGate setup guides with confidence._

---

## Who this is for

If words like **VPS**, **SSH**, or **IPv6** make your eyes glaze over—this is your warm-up lap. We’ll show you that renting a tiny server on the internet is easy, cheap, and surprisingly powerful.

> **TL;DR:** You’ll spin up a Linux box (Ubuntu), connect to it with PuTTY (or your terminal), change the initial root password, add basic safety, and save a reusable login. From there, you’ll jump into the TruthGate install guides.

---

## What you’re actually getting

- **A Linux box on the internet**: Think of it as a tiny computer you rent by the month.
    
- **Ubuntu LTS**: Choose **Ubuntu 24.04 LTS** (or 22.04 LTS). Stable, common, well-supported. _We suggest Ubuntu—always._
    
- **Two addresses**:
    
    - **IPv4** (the one you’re used to seeing, like `123.45.67.89`).
        
    - **IPv6** (the modern format, like `2a01:4f8:...`). You don’t need to understand it—just know it’s normal.
        
- **Root account**: The master admin on the box. Your provider will email you a password **or** show it in their control panel. You’ll change it at first login.
    

---

## Pick a provider (Hetzner or NetCup)

Both are crazy affordable and totally fine for TruthGate.

**Hetzner Cloud**

- Create a project → Create server
    
- Image: **Ubuntu 24.04 LTS**
    
- Region/Location: pick something near you or your audience
    
- Size: a small plan (2–4 vCPU, 4–8GB RAM) is usually plenty to start
    
- Networking: leave defaults; keep IPv6 enabled
    
- Auth: If you don’t have an SSH key yet, start with a **password** (we’ll fix keys later)
    

**NetCup vServer**

- Similar flow: pick a plan, choose **Ubuntu 24.04 LTS**, deploy
    
- Password usually arrives by email (or visible in the panel)
    
- Keep the email handy; we’ll change the password immediately after login
    

> **Turn on 2FA** in your provider account. It’s your keys to the kingdom.

---

## Before you connect: password + tools

- **Password manager**: Install something like **KeePass** / **KeePassXC** (or your favorite). Create a vault entry for this server.
    
    - Generate a **long, random** password for root (we’ll set it in a moment).
        
- **Windows users**: Install **PuTTY** (and optionally **PuTTYgen** for keys later).
    
- **macOS/Linux users**: You already have the `ssh` command in Terminal.
    

---

## Find your server’s IP & initial password

- Your provider will **email** the IP (IPv4/IPv6) and a temporary **root password**, or show them in the control panel.
    
- Put the IP and password into your password manager entry now.
    

---

## Connect (Windows via PuTTY)

1. Open **PuTTY**.
    
2. **Host Name (or IP address)**: paste your server’s **IPv4**.
    
3. **Port**: `22` • **Connection type**: `SSH`.
    
4. In **Saved Sessions**, type a friendly name (e.g., `TruthGate VPS`) → click **Save**.
    
5. Click **Open**.
    
6. First-time popup (“server’s host key”): click **Accept**.
    
7. Login as: `root` → Press **Enter**
    
8. When prompted for the password, **right-click** to paste (PuTTY uses right-click) → **Enter**.
    

> If the password seems to “not type,” that’s normal—Linux hides the characters. Just paste and press Enter.

---

## Connect (macOS / Linux Terminal)

```bash
ssh root@YOUR.IPV4.ADDRESS
```

- On first connect: type `yes` to trust the host key.
    
- Paste the password (Cmd+V on macOS Terminal usually works; otherwise use Edit → Paste) and press Enter.
    

---

## Copy & paste tips (no more weirdness)

**Passwords are invisible while typing** in Linux—this is normal.

- **PuTTY (Windows)**
    
    - **Paste**: Right‑click **or** **Shift+Insert**
        
    - **Copy**: Select text (auto‑copies) **or** **Ctrl+Insert**
        
    - Tweak behavior in _Settings → Window → Selection/Mouse_ if you prefer.
        
- **Windows Terminal / PowerShell / Cmd**
    
    - **Paste**: **Ctrl+V** (modern terminals) **or** **Shift+Insert**
        
    - **Copy**: **Ctrl+C** when text is selected **or** **Ctrl+Insert**
        
    - Note: Inside SSH, **Ctrl+C** sends an interrupt to the remote program; select text first if you intend to copy.
        
- **macOS Terminal / iTerm2**
    
    - **Paste**: **⌘V** (Command+V) _(Shift+Insert may also work in some setups)_
        
    - **Copy**: **⌘C**
        
- **Linux terminals (GNOME, Konsole, etc.)**
    
    - **Paste**: **Ctrl+Shift+V** _(or_ **Shift+Insert** _or middle‑click)_
        
    - **Copy**: **Ctrl+Shift+C**
        
- **Pro tip**: Middle‑click often pastes the most recently _selected_ text on Linux.
    

> If a command pastes on the same line as your prompt, add `; echo` to force a newline after it.

$1  
**1) Change the root password**

```bash
passwd
```

- Enter the current one (from email), then paste your **new** long, random password from your password manager—twice.
    

**2) Update the box**

```bash
apt update && apt upgrade -y
```

**3) Optional but smart: set your timezone**

```bash
timedatectl list-timezones | less   # find yours
sudo timedatectl set-timezone America/New_York  # example
```

---

## Basic safety (quick and friendly)

We’ll keep this lightweight so nobody gets stuck.

**Install and turn on UFW (firewall)**

```bash
apt install -y ufw
ufw allow OpenSSH     # keeps your current SSH connection working
ufw enable            # type 'y' when asked
ufw status            # just to verify
```

> This only allows SSH for now. We’ll open web ports (80/443) during TruthGate setup.

**Enable automatic security updates**

```bash
apt install -y unattended-upgrades
sudo dpkg-reconfigure --priority=low unattended-upgrades
```

> That’s it—you’re already safer than most boxes on the internet.

---

## Save your login for next time

**PuTTY (Windows)**

- In the left tree, expand **Connection → Data** and set **Auto-login username** to `root`.
    
- (Optional) Later, when we add keys, you’ll go to **Connection → SSH → Auth**, set your private key file, and **Save** again.
    
- Return to **Session**, select your saved name, and **Save**.
    

**Terminal (macOS/Linux)**

- Create a simple alias in your shell profile (optional):
    

```bash
echo "alias truthgate='ssh root@YOUR.IPV4.ADDRESS'" >> ~/.bashrc
source ~/.bashrc
# Now you can just type: truthgate
```

---

## (Optional) SSH keys: future-you will love this

Passwords work, but SSH keys are smoother and safer. We’ll fully cover key-based logins in the TruthGate guides, but here’s the gist:

**macOS/Linux**

```bash
ssh-keygen -t ed25519 -C "yourname@truthgate"
cat ~/.ssh/id_ed25519.pub
```

Copy that line to your clipboard; we’ll add it to `~/.ssh/authorized_keys` on the server later.

**Windows**

- Either use **PuTTYgen** to make a keypair (`ed25519`), or use the built-in OpenSSH:
    

```powershell
ssh-keygen -t ed25519 -C "yourname@truthgate"
```

- We’ll walk you through attaching the key in the next guide and then disabling password SSH if you want maximum safety.
    

---

## Quick sanity checks

**See your server’s public IPs from the box**

```bash
curl -4 ifconfig.me ; echo
curl -6 ifconfig.me ; echo
```

**Check connectivity**

```bash
ping -c 3 1.1.1.1
```

**Check disk space**

```bash
ds -h      # if not found: apt install -y moreutils  (or use 'df -h')
```

---

## Common hiccups (and easy fixes)

- **Access denied / wrong password**: Re-copy carefully; PuTTY right-click pastes. Providers sometimes force reset in the panel—check for a “Reset root password” option.
    
- **Black screen / nothing happens**: Your local network/firewall may block outbound port 22. Try mobile hotspot or another network.
    
- **Host key changed warning**: You rebuilt the server; PuTTY/SSH thinks it’s suspicious. In PuTTY, delete the old entry under _Known Hosts_. On macOS/Linux, edit `~/.ssh/known_hosts` and remove the line for that IP.
    
- **IPv6 confusion**: Ignore it for now; IPv4 is enough. You can add IPv6 later.
    
- **UFW locked you out**: Always `ufw allow OpenSSH` _before_ `ufw enable`.
    

---

## What’s next

You’re connected, updated, and lightly hardened. Perfect. From here, hop to the **TruthGate setup** guides:

1. **Install IPFS (Docker or native)** – beginner-friendly path with Docker available.
    
2. **Install TruthGate (native on the server)** – ensures the environment can run the `ipfs` CLI where needed.
    
3. **Open web ports (80/443) and go live** – we’ll guide UFW rules at that stage.
    

Keep your password manager entry up to date, and consider moving to SSH keys when you’re ready. That’s it—you’re officially “VPS fluent.” 👏

---

## Pocket checklist (copy/paste)

-  Provider account has **2FA** enabled
    
-  Server: **Ubuntu 24.04 LTS** (IPv4 & IPv6 ok)
    
-  Connect via SSH (PuTTY or Terminal) as **root**
    
-  `passwd` to set a **long** new root password (save in KeePass)
    
-  `apt update && apt upgrade -y`
    
-  `apt install -y ufw && ufw allow OpenSSH && ufw enable`
    
-  `apt install -y unattended-upgrades` and enable
    
-  Save your PuTTY session / create a terminal alias
    
-  Ready for **TruthGate** install


[Click here to head over to the TruthGate Setup Guide](./docs/setup)