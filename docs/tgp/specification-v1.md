# TruthGate Pointer protocol — wire version 1

**Status:** Open specification  
**Wire version:** `1`

The key words **MUST**, **MUST NOT**, **SHOULD**, **SHOULD NOT**, and **MAY** indicate conformance requirements.

## 1. Root document

A conforming TGP v1 IPNS root MUST provide:

```text
/tgp.json
```

The path is relative to the root resolved by the IPNS identity.

## 2. Media type and encoding

`tgp.json` MUST be valid UTF-8 JSON.

Servers SHOULD use:

```text
Content-Type: application/json
```

## 3. Object schema

Example:

```json
{
  "tgp": 1,
  "ts": "2026-07-25T17:00:00Z",
  "current": "bafy...",
  "domainName": "example.com",
  "legal": "/legal.md"
}
```

### `tgp`

- Type: integer
- Required: yes
- Value for this specification: `1`

A client MUST reject an unsupported wire version.

### `ts`

- Type: string
- Required: yes
- Format: RFC 3339 / ISO 8601 UTC timestamp

The timestamp indicates when the publisher generated the pointer. It is freshness metadata, not a replacement for IPNS sequence validation.

### `current`

- Type: string
- Required: yes

Accepted forms:

```text
bafy...
/ipfs/bafy...
```

The target MUST identify an IPFS content path. A v1 client MUST NOT invent a target when this field is absent or empty.

### `domainName`

- Type: string
- Required: no

The primary associated DNS name. Clients MUST NOT treat this unsigned field separately from the trust provided by the resolved IPNS record.

### `legal`

- Type: string
- Required: no

A path to an informational notice, normally:

```text
/legal.md
```

The notice MUST NOT be required for pointer resolution.

### Unknown fields

Clients MUST ignore unknown fields so compatible metadata can be added without breaking v1 readers.

## 4. Resolution

A client MUST:

1. resolve the intended IPNS identity;
2. fetch `/tgp.json`;
3. parse JSON;
4. verify `tgp` is supported;
5. verify `ts` and `current` exist;
6. normalize `current`;
7. load the IPFS target.

Normalization:

- if `current` begins with `/ipfs/`, use it;
- otherwise treat it as a CID and prepend `/ipfs/`.

A client MUST NOT interpret arbitrary URL schemes from `current`.

## 5. Caching

The pointer is mutable and SHOULD use a short freshness lifetime. Sixty seconds is a reasonable default when no more specific policy exists.

The immutable target MAY be cached according to normal IPFS and HTTP policy.

A client MAY retain a last-known-good target for availability, but SHOULD indicate when it is using stale state.

## 6. Failure behavior

- missing document: fail or use an explicitly identified last-known-good target;
- malformed JSON: fail;
- unsupported version: fail;
- missing or empty `current`: fail;
- invalid target: fail;
- missing legal notice: continue resolution.

Clients MUST NOT guess a CID from unrelated files.

## 7. Optional files

```text
/index.html
/legal.md
```

`index.html` MAY provide browser-oriented resolution.

`legal.md` MAY provide operator information and limitations.

## 8. JSON Schema

```json
{
  "$schema": "https://json-schema.org/draft/2020-12/schema",
  "title": "TruthGate Pointer v1",
  "type": "object",
  "additionalProperties": true,
  "required": ["tgp", "ts", "current"],
  "properties": {
    "tgp": {
      "type": "integer",
      "const": 1
    },
    "ts": {
      "type": "string",
      "format": "date-time"
    },
    "current": {
      "type": "string",
      "minLength": 3
    },
    "domainName": {
      "type": "string"
    },
    "legal": {
      "type": "string"
    }
  }
}
```

## 9. Conformance checklist

- [ ] `/tgp.json` exists at the IPNS root.
- [ ] JSON is valid UTF-8.
- [ ] `tgp` is integer `1`.
- [ ] `ts` is a date-time string.
- [ ] `current` is a CID or `/ipfs/` path.
- [ ] unknown fields do not break clients.
- [ ] mutable pointer caching is short.
- [ ] clients fail safely on malformed data.
