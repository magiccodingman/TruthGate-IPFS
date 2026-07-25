# TruthGate Linux Setup Guide

The following is the guide to setup TruthGate on a Linux server.

Before we get started, if you appreciate this project, drop a star on the GitHub repo to show some love!
[Click here to see GitHub Repo](https://github.com/TruthOrigin/TruthGate-IPFS)

If you have any issues or errors during the process. Please leave a ticket on the GitHub page linked at the bottom of the footer of this site. But please be sure you've really followed every step of the instructions before doing so.

## Base OS prep (Ubuntu 24.04)

```bash
sudo apt update && sudo apt upgrade -y
sudo apt install -y curl wget git nano ca-certificates lsb-release software-properties-common
```

Create directories TruthGate will use:

```bash
sudo mkdir -p /opt/truthgate/
sudo mkdir -p /opt/truthgate/certs
sudo chown -R "root":"root" /opt/truthgate /opt/truthgate/certs
```

> Switch "root" with your actual user if you're not being lazy


---

## Install IPFS

I suggest installing IPFS via docker. It makes the process significantly easier.

First run the following to enable secure ports:
```bash
apt install ufw -y
sudo ufw allow 4001/tcp
sudo ufw allow 4001/udp
sudo ufw allow 80/tcp 
sudo ufw allow 443/tcp
```


Then run for docker:
```bash
curl -fsSL https://get.docker.com | sh
sudo apt-get install -y docker-compose-plugin
```

then we'll be making a project folder and compose the file:
```bash
mkdir -p ~/ipfs-docker
cd ~/ipfs-docker
```

Then copy and paste this YAML script that's going to set everything up for you:
```bash
cd ~/ipfs-docker
rm -f docker-compose.yml
cat > docker-compose.yml <<'YAML'
services:
  ipfs:
    image: ipfs/kubo:latest
    container_name: ipfs
    restart: unless-stopped
    network_mode: host
    volumes:
      - ipfs_repo:/data/ipfs
    entrypoint: ["/bin/sh","-c"]
    command: ["set -e; \
if [ ! -f /data/ipfs/config ]; then ipfs init --profile server; fi; \
V4=\"$$(curl -4 -s --max-time 3 ifconfig.me || true)\"; \
V6=\"$$(curl -6 -s --max-time 3 ifconfig.me || true)\"; \
JSON='['; FIRST=1; \
add_ann(){ if [ $$FIRST -eq 0 ]; then JSON=\"$$JSON,\"; fi; JSON=\"$$JSON\\\"$$1\\\"\"; FIRST=0; }; \
if [ -n \"$$V4\" ]; then add_ann \"/ip4/$$V4/tcp/4001\"; add_ann \"/ip4/$$V4/udp/4001/quic-v1\"; fi; \
if [ -n \"$$V6\" ]; then add_ann \"/ip6/$$V6/tcp/4001\"; add_ann \"/ip6/$$V6/udp/4001/quic-v1\"; fi; \
if [ $$FIRST -eq 0 ]; then JSON=\"$$JSON]\"; ipfs config --json Addresses.AppendAnnounce \"$$JSON\"; fi; \
ipfs config --json Addresses.Swarm \"[\\\"/ip4/0.0.0.0/tcp/4001\\\",\\\"/ip4/0.0.0.0/udp/4001/quic-v1\\\",\\\"/ip6/::/tcp/4001\\\",\\\"/ip6/::/udp/4001/quic-v1\\\"]\"; \
ipfs config --json API.HTTPHeaders.Access-Control-Allow-Origin \"[\\\"*\\\"]\"; \
ipfs config --json API.HTTPHeaders.Access-Control-Allow-Methods \"[\\\"GET\\\",\\\"POST\\\",\\\"PUT\\\",\\\"DELETE\\\",\\\"OPTIONS\\\"]\"; \
ipfs config --json API.HTTPHeaders.Access-Control-Allow-Headers \"[\\\"*\\\"]\"; \
ipfs config --json Addresses.Gateway '\"/ip4/0.0.0.0/tcp/9010\"'; \
ipfs config Routing.Type dhtserver; \
ipfs config --json Swarm.EnableHolePunching true || true; \
exec ipfs daemon --migrate=true"]
volumes:
  ipfs_repo:
YAML
```


Then run the following to run docker:
```bash
# Start in the background
docker compose up -d

# Watch logs for ~30–60s to see it settle
docker compose logs -f

# Set alias for TruthGate calls
alias ipfs='docker exec -it ipfs ipfs'
source ~/.bashrc
```


Validate IPFS is working and you should see your peer ID:
```bash
ipfs id -f="<id>\n"
```


I also suggest being extra safe and on another PC with a different IP address, download the IPFS desktop app if you don't have it already. Then run:
```bash
ipfs routing findpeer <Your_Peer_Id>
```

And validate that you see something like:
```
/ip6/YourIpv6Ip/udp/4001/quic-v1
/ip4/YourIpv4Ip/tcp/4001
/ip6/YourIpv6Ip/tcp/4001
/ip4/YourIpv4Ip/udp/4001/quic-v1
```

This will validate that your node is running properly as a DHT server.

Lastly to setup IPFS in your Linux box, you should run:
```
ipfs config Datastore.StorageMax "<Max_GB>GB"
docker restart ipfs
```

Replace the `<Max_GB>` with how much storage space in GB you'd like to allocate to IPFS. I am utilizing NetCup for this build, so I have ~256 GB by default, so I'll allocate 200 GB. IPFS tries to keep it within this range, but if it runs out, it'll begin garbage collection.


That's it!  Now we can go onto the next step to install TruthGate.

## Install TruthGate

Finally we can start installing TruthGate. We're almost there, I promise!

### Setup Dot Net

To get started installing TruthGate, first [Click this link here to see the framework version - https://github.com/TruthOrigin/TruthGate-IPFS/blob/master/TruthGate-Web/TruthGate-Web/TruthGate-Web.csproj](https://github.com/TruthOrigin/TruthGate-IPFS/blob/master/TruthGate-Web/TruthGate-Web/TruthGate-Web.csproj). 

In which in that link you'll see something like:
```xml
<TargetFramework>netX.X</TargetFramework>
```

That netX.X is the current project NET version. This will update over time, so if future builds fail, come back to this step to make sure you install the latest SDK framework.

Type into your linux command line:
```bash
lsb_release -a
```

And this'll tell you which version of Ubuntu you're on or if you're using something like Debian.

**If you're on Ubuntu:**
Note that NetCup usually starts you with Debian so this isn't for you.
But now [go to this link https://learn.microsoft.com/dotnet/core/install/linux-ubuntu-install](https://learn.microsoft.com/dotnet/core/install/linux-ubuntu-install). 

And click on the Ubuntu Version you're working with, scroll down and find the tabs that says "Net 9", "Net 10", and so on. Click the NET version that the project is currently using that you saw in the target framework. Then simply copy and paste the ~2 commands to install the SDK.

**For Debian:**
For those of us on Debian which is my case right now utilizing NetCup, go here:
[go to this link https://learn.microsoft.com/en-us/dotnet/core/install/linux-debian](https://learn.microsoft.com/en-us/dotnet/core/install/linux-debian). 
**Only for those on X86! I chose the NetCup ARM64, so instructions for that is down below.**

Then follow the instructions for your Debian version to install the wget and rm package, and then click the NET version and copy and paste those commands to install the SDK.

```bash
sudo apt-get update

# Install prerequisites
sudo apt-get install -y wget gpg

# Download the official installer
wget https://dot.net/v1/dotnet-install.sh -O /tmp/dotnet-install.sh
chmod +x /tmp/dotnet-install.sh

sudo mkdir -p /usr/share/dotnet
sudo /tmp/dotnet-install.sh --channel 9.0 --architecture arm64 --install-dir /usr/share/dotnet

# Make sure it's on PATH for all shells
echo 'export PATH="/usr/share/dotnet:$PATH"' | sudo tee /etc/profile.d/dotnet-path.sh >/dev/null
source /etc/profile.d/dotnet-path.sh

# Verify
dotnet --info
dotnet --list-sdks
```

This is for the NET 9, but where it says 9.0 next to `--channel` just change that to the current version being used.

**Note:** Installing the SDK of NET can be annoying in Linux. Your friendly AI often can help guide you through any issues, but be sure to utilize the official Microsoft resources for installation. The AI likes to do direct deb inserts and so on. If you don't know what that means, just copy and paste this into your AI prompt, "Please use official microsoft or other guides, don't utilize deb based inserts for this process to setup the NET SDK."

Finally with everying installed run the following:
```bash
sudo apt-get update
sudo apt-get install -y libatomic1 libstdc++6 libgcc-s1 zlib1g libicu-dev python3 build-essential clang
# Optional but nice to have
dotnet workload install wasm-tools
```


---

### TruthGate Repo Install

Now we can move forward with cloning down the GitHub repo for TruthGate and the super handy script I packed into this to make things easier:
```bash
cd ~
rm -rf TruthGate-IPFS
git clone https://github.com/TruthOrigin/TruthGate-IPFS.git
sudo cp ~/TruthGate-IPFS/truthgate-run.sh /usr/local/bin/truthgate-run.sh
sudo chmod +x /usr/local/bin/truthgate-run.sh
```


Now we're going to create the service that's going to run and control TruthGate. 

Create the service file:

```bash
sudo nano /etc/systemd/system/truthgate.service
```

Then paste the following:

Paste:

```ini
[Unit]
Description=TruthGate (dynamic TFM + RID)
After=network-online.target
Wants=network-online.target

[Service]
Type=simple
# Safer: run as a dedicated user (create it first)
# User=truthgate
# Environment=HOME=/opt/truthgate
# If you insist on root (not recommended), set: User=root
User=root

# Make dotnet available under systemd
Environment=DOTNET_ROOT=/usr/share/dotnet
Environment=PATH=/usr/share/dotnet:/usr/bin:/bin
Environment=ASPNETCORE_URLS=http://0.0.0.0:7175

# App settings
Environment=PROJECT_ROOT=/root/TruthGate-IPFS
Environment=SELF_CONTAINED=true
Environment=WASM_NATIVE=false
Environment=TRUTHGATE_CERT_PATH=/opt/truthgate/certs
Environment=TRUTHGATE_CONFIG_PATH=/opt/truthgate/config.json
Environment=ASPNETCORE_ENVIRONMENT=Production
Environment=DOTNET_ENVIRONMENT=Production
Environment=AUTO_UPDATE=true

# ACME staging toggle (dev only)
Environment=TRUTHGATE_ACME_STAGING=false

# Optional: override Kestrel if needed
# Environment=ASPNETCORE_URLS=http://0.0.0.0:7175

ExecStart=/usr/local/bin/truthgate-run.sh
Restart=always
RestartSec=5
SyslogIdentifier=truthgate

[Install]
WantedBy=multi-user.target
```

> For those not familiar with Linux, to save type `Ctrl-O` and then `enter`, then `Ctrl-X`'
> 
> **Security note:** Running as `root` is easy but not best practice. If you switch to a dedicated user, also move `PROJECT_ROOT`, `HOME`, and file permissions accordingly. Do as I say, not as I do 😉


Also note that the setting `Environment=AUTO_UPDATE=true` means that TruthGate will auto update every time you restart the service. You can turn that to `false` if this is not what you'd like to happen.

Enable and start:

```bash
sudo systemctl daemon-reload
sudo systemctl enable truthgate
sudo systemctl start truthgate
journalctl -u truthgate -f
```

This might take a while

---

And then there's a million more steps....

Just joking!

Last command, run this!
```bash
curl -4 ifconfig.me; echo
```

You're going to see your IPV4 address. take that and run in your browser:
```
https://{Your_IPv4}
```

And you're going to see the TruthGate login screen. 

- Username: **`admin`**    
- Password: **`LetMeInBro123`**

Login for the first time and immediately click the **Settings** tab, then **Users**. Where you see the admin username, click the **Change Password** button and immediately change the password to something secure.

For your TruthGate connection, you'll see a warning on the browser due to the SSL. The SSL is safe, it's self signed, and it's not necessary to have anything else for this login. Most browsers let you click "Advanced" then "Proceed".

And there you have it! Welcome to TruthGate!

If you are utilizing TruthGate to publish domains, please go to and read the:
[Site Publishing Guide](./docs/site-publishing)

To learn how to utilize the UI and settings, please go to:
[TruthGate Docs](./docs)


Otherwise, you're good to go! Please consider pinning the IPNS links shown below :D
