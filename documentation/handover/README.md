# Handover Docs

Presentation-style briefing docs for team knowledge transfer — deeper and more narrative than an architecture doc, meant to be presented from or read end-to-end. Maintained by humans; update in place as understanding evolves rather than creating a new file per revision.

These are standalone `.html` files (open directly in a browser) rather than Markdown, since they're built for presenting and include diagrams.

| Doc | Covers |
|---|---|
| [`Redis-Unity-Bridge.html`](Redis-Unity-Bridge.html) | How the Unity plugin reads, writes, and reconciles data through Redis and RabbitMQ — cache-aside reads, optimistic writes, outbox/inbox, ack/nack (transport and business level), upfront validation, and a proposed TBD mechanism for Unity-initiated cache invalidation |
