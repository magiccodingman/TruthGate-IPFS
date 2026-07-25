# Informational TGP notice template

This is a technical starting point, not legal advice or a jurisdiction-specific policy. Have qualified counsel review any notice used in production.

```markdown
# Current-content pointer notice

This IPNS identity uses the TruthGate Pointer protocol (TGP) to identify one current IPFS content target.

The current target is declared in `/tgp.json`.

The operator may update that target, remove local pins, and run local repository garbage collection. These actions affect the operator's own systems. They do not delete or control copies that may have been retained by independent IPFS nodes, gateways, caches, mirrors, archives, or users.

A CID is content-addressed and may remain retrievable from third parties after the operator stops advertising or retaining it.

For questions or reports concerning the current target, contact:

- Operator: [NAME]
- Contact: [CONTACT METHOD]
- Policy or jurisdiction information: [OPTIONAL]

Last updated: [DATE]
```

## Usage

1. Save the reviewed notice as `/legal.md` in the TGP root.
2. Add:

```json
{
  "legal": "/legal.md"
}
```

to `tgp.json`.
3. Keep the notice contact information current.
4. Do not claim that the notice changes third-party behavior.
