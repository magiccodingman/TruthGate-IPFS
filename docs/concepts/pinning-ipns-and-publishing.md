# Pinning, IPNS, and publishing

![Watched IPNS pins](../../content/images/demo/pinned-ipns-dark.webp)

## CID pins

A CID identifies immutable content. Pinning tells the local Kubo repository to retain the reachable blocks instead of allowing garbage collection to remove them.

## IPNS

IPNS gives a stable, signed identity whose value can point to different content over time.

An IPNS name is not itself a local retention policy. Resolving a name does not guarantee that your node keeps the target.

## Watched IPNS pinning

TruthGate can subscribe to an IPNS identity.

A background worker:

1. resolves the name;
2. determines the current target;
3. pins the target;
4. records state;
5. applies configured retention behavior;
6. repeats after the scheduled cooldown.

This is useful for mirrors, deployment nodes, regional replicas, and applications that need the current version available locally.

## Retention choices

Two broad policies exist:

- retain previously observed targets;
- keep the current target and unpin older targets.

Neither policy can control blocks held by third parties.

## Publishing

Publishing creates content and optionally updates a mutable identity you control.

Watching and publishing can be combined:

- one TruthGate instance publishes;
- other instances watch the IPNS name;
- each watcher pins the current CID.

## TGP

TGP makes the mutable IPNS payload a small pointer to the current CID instead of making the IPNS root the complete site.

Read [TGP rationale](../tgp/rationale.md).
