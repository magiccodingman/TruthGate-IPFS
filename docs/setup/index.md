# TruthGate Setup

TruthGate is designed to run as a **secure edge gateway** alongside your IPFS node. While it’s technically possible to install on a personal desktop, the **recommended setup** is on a VPS or isolated Linux VM. This separation ensures stability, performance, and security.

## Recommended Providers

- **NetCup** – For most people, NetCup’s ARM servers are the best value.    
    - Example: [NetCup ARM VPS 1000 G11](https://www.netcup.com/en/server/arm-server/?ref=283649)    
        - 6 vCPU (shared) • 8 GB RAM • 256 GB NVMe • ~~€5.26 (~~$6.14)/month            
    - They also offer inexpensive **Local Block Storage (LBS)** add-ons. Note: NetCup’s LBS uses SAS drives, which are slower than Hetzner’s NVMe-based LBS.  
    - Though note you can expand the NVME storage at NetCup as well.
    - Offers geographic hosting in Austria for world class internet privacy laws.
- **Hetzner** – Slightly more expensive but comes with advantages.    
    - Example: Shared vCPU ARM64 CAX21 (eu-central, Helsinki)        
        - 4 vCPU • 8 GB RAM • 80 GB NVMe • 20 TB traffic • ~$6.99/month            
    - Benefits:        
        - **Faster LBS storage** (NVMe) for scaling IPFS nodes.            
        - Easy upgrades to **dedicated vCPUs** if you need stronger performance.            
        - Modern, user-friendly UI and management tools.
        - Offers geographic hosting in Finland (Helsinki) for world class internet privacy laws.

**Rule of thumb:**

- Go with **NetCup** if you want more cores and storage per dollar.
    
- Choose **Hetzner** if you value faster storage and a smoother management experience.

---

**Please Use My Affiliate Links:**
I suggest Hetzner and NetCup genuinely because that's what I use. But I also created and open sourced this entire project alone as a solo developer. By simply using these links to access Hetzner or NetCup, it will cost you nothing and I would greatly appreciate it.

[NetCup - https://www.netcup.com/en/?ref=283649](https://www.netcup.com/en/?ref=283649)

[Hetzner - https://hetzner.cloud/?ref=HzjqvQhGt5H3](https://hetzner.cloud/?ref=HzjqvQhGt5H3) - Get a $20 cloud credit

From my experience as well, Hetzner tends to be really fast. But those of us used to the WebAssembly world, what's a few more seconds to save some money with NetCup? Either way, it's up to you.

Thank you :)

---

## Operating System

TruthGate is **best supported on Ubuntu**.

- ✅ Ubuntu 24.04+ is the recommended baseline.
    
- ⚠️ Windows is technically supported by the codebase, but setup is more complex, requires extra configuration, and is **not covered in this guide**.
    

---

## 3. Why TruthGate Cannot Be Dockerized

While Docker would make installation dead simple, TruthGate must run directly in the **host environment** where IPFS is installed. Here’s why:

- TruthGate communicates with IPFS primarily through the **HTTP API**.
    
- However, critical operations like **importing and exporting IPNS keys** are **not exposed by the IPFS API**.
    
- For these cases, TruthGate needs **direct access to the `ipfs` CLI** inside the same environment as your node.
    

> **Bottom line:** Run IPFS in Docker if you like, but TruthGate itself must be installed **on the host VM** with command-line access to IPFS.

---

## 4. Hardware Requirements

- **Memory**: 8 GB minimum (sufficient for both TruthGate and IPFS).
    
- **Storage**: Check your VPS’s default NVMe allocation.
    
    - If you expect heavy usage, consider attaching **Local Block Storage (LBS)**.
        
    - Recommended strategy: add **8 volumes** and stripe them with **LVM**. This lets you scale IPFS storage easily in the future.
        
    - (Need help with LVM striping? Any AI assistant or sysadmin guide can provide the exact commands.)
        

---

## 5. Default Credentials (Change Immediately!)

After installation:

- Username: **`admin`**
    
- Password: **`LetMeInBro123`**
    

⚠️ Log in and change this password immediately.

If you need to reset the admin password later:

- Edit the TruthGate config file.
    
- Insert this reset token:
    

```
/EUxvODjrpkTKnal6nVEAh2m+52H4OgXGEBLcE3xilcgZ8gbeE5ay/CfzYr9PCJ0
```

This will reset the password back to **`LetMeInBro123`**.

---

## 6. Managed IPFS Paths (Hands Off!)

TruthGate manages specific MFS paths inside your IPFS node. **Do not touch these:**

- `/production/**`
    
- `/staging/**`
    

Tampering with these paths will break TruthGate’s publishing system.

---

## 7. Once Setup is Complete

After setup, TruthGate is **straightforward and reliable** — a true secure edge gateway for IPFS. If you can type basic commands, you’re golden.

If you're familiar with a VPS and setting up a Linux Box, skip to this step when you're ready to setup your environment:  
[The Setup Guide (Click Here)](./docs/setup/linux) for the **step-by-step installation guide**.


If "VPS" or Linux containers or VM's are unfamiliar terminology to you. Don't worry!
[Click here to read the VPS On Ramp guide for beginners](./docs/setup/vps-on-ramp)