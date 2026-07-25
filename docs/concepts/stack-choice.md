# Philosophy of Stack Choice

When people see that TruthGate is built on **C# / ASP.NET / Blazor Hybrid**, I know some eyes immediately squint. _Microsoft in Web3? Really?_  
So let’s get this out of the way up front:

- **TruthGate is not decentralized software.**
    
- **TruthGate does not replace IPFS.**
    
- **TruthGate is the bridge, the personal edge gateway that works _with_ IPFS, not against it.**
    

And because it’s a bridge, the philosophy behind the stack matters less about ideology and more about **results**.

---

## Protocol First, Stack Second

TruthGate speaks in **open protocols**.

- It publishes websites onto **IPFS**.
    
- It manages **IPNS** tracking.
    
- It supports the **TruthGate Pointer Protocol (TGP)** for automation.
    

None of those depend on .NET, Blazor, or Microsoft. The stack is an implementation detail. The decentralized core remains decentralized.

---

## Why C# / ASP.NET

I chose C# and ASP.NET because:

- **Mature async I/O & threading** – predictable under load.
    
- **Enterprise-grade reliability** – tracing, logging, caching, retries.
    
- **Battle-tested HTTP** – this is an API-heavy system; APIs are what .NET does best.
    
- **My fluency** – I know this stack inside and out, which means faster iteration and fewer mistakes.
    

Could it have been Rust or Go? Sure. But this layer isn’t the hot path of IPFS cryptography, it’s the ops glue and integration logic. And for that, boring reliability beats bleeding-edge flair.

---

## Why Blazor Hybrid

TruthGate includes a **WebUI** built with Blazor Hybrid. Let’s be clear about what that means:

- It’s the **console for _you_**, the operator of your personal edge gateway.
    
- It is _not_ the protocol, it is _not_ a dApp, and it does not affect the decentralization of your published content.
    
- TruthGate can be run **headless**, driven entirely by API calls and automation if you prefer.
    

I chose Blazor Hybrid because it allowed me to quickly ship a solid, integrated console without reinventing the wheel.

---

## The Bridge, Not the Blockchain

TruthGate’s role is not to be another “decentralized app.” Its role is to:

- **Automate** the pain points of IPFS and IPNS.
    
- **Simplify** the operational overhead.
    
- **Integrate** with Web2 workflows so Web3 isn’t intimidating.
    
- **Publish** sites that are actually decentralized when they hit IPFS.
    

This is the tool I believe should have always existed: a **personal edge gateway** that makes IPFS approachable.

---

## Adoption Over Philosophy

I respect the philosophy of decentralization. But philosophy alone doesn’t drive adoption. Results do.  
TruthGate is pragmatic:

- It delivers working integrations today.
    
- It lowers the barrier for new projects to adopt Web3.
    
- It keeps the decentralized parts decentralized while giving enterprises and individuals a reliable gateway in front.
    

If that makes it “corporate” because it leans on .NET? Then good. Reliability is what bridges need.
